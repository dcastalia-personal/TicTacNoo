using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitSuccessAckSys : ISystem {
    EntityQuery query;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SuccessAckData, RequireInitData>() );
        state.RequireForUpdate( query );
    }

    public void OnUpdate( ref SystemState state ) {
        var ui = GetSingleton<InGameUIData>();
        var gameState = GetSingleton<GameStateData>();
        var gameStateEntity = GetSingletonEntity<GameStateData>();
        var successAck = GetSingleton<SuccessAckData>();
        var successPanel = successAck.successUIPrefab.Value.Instantiate();
        ui.document.Value.rootVisualElement.Add( successPanel );
		
        var continueButton = successPanel.Q<Button>( "Continue_Button" );
        var abortButton = successPanel.Q<Button>( "Abort_Button" );

        var successAckEntity = GetSingletonEntity<SuccessAckData>();
        var successAnimationPrefab = gameState.successAnimationPrefab;
        
        var entityManager = state.EntityManager;
        var levelCount = GetBuffer<Level>( gameStateEntity ).Length;

        SetComponentEnabled<VictoryEnabled>( gameStateEntity, true );
        
        continueButton.clicked += () => {
            // unload current scene and reload
            entityManager.DestroyEntity( successAckEntity );
            entityManager.Instantiate( successAnimationPrefab );
            ui.document.Value.rootVisualElement.Remove( successPanel );
            gameState.nextLevel = gameState.curLevelIndex + 1;
            if( gameState.nextLevel > levelCount - 1 ) gameState.nextLevel = 0; // skip the "common" scene, since it will have already been loaded
            entityManager.SetComponentData( gameStateEntity, gameState );
            
            var matchInfoDisplay = ui.document.Value.rootVisualElement.Q<VisualElement>( "Match_Info" );
            matchInfoDisplay.AddToClassList( "out" );
        };
		
        abortButton.clicked += () => {
            // unload current scene and load main menu
            entityManager.DestroyEntity( successAckEntity );
            entityManager.Instantiate( successAnimationPrefab );
            ui.document.Value.rootVisualElement.Remove( successPanel );
            gameState.nextLevel = 1; // main menu
            entityManager.SetComponentData( gameStateEntity, gameState );
            
            var matchInfoDisplay = ui.document.Value.rootVisualElement.Q<VisualElement>( "Match_Info" );
            matchInfoDisplay.AddToClassList( "out" );
        };

        var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

        ecb.Instantiate( successAck.successAckAudio );
    }
}

public partial struct SuccessAnimationSys : ISystem {
    EntityQuery particlesQuery;
    EntityQuery animatedShapesQuery;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        state.RequireForUpdate<SuccessAnimData>();
        particlesQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StarParticleData>() );
        animatedShapesQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RandomAnimModifierData>() );
    }

    [BurstCompile] public void OnUpdate( ref SystemState state ) {
        var cameraEntity = GetSingletonEntity<CameraData>();
        var cameraTransform = GetComponent<LocalToWorld>( cameraEntity );
        var deltaTime = SystemAPI.Time.DeltaTime;
        var animationData = GetSingletonRW<SuccessAnimData>();

        if( animationData.ValueRO.timeElapsed == 0f ) {
            var targetColorData = GetComponent<TargetColorData>( cameraEntity );
            targetColorData.baseColor = GetSingleton<GameColorData>().neutralBackground;
            SetComponent( cameraEntity, targetColorData );
            SetComponentEnabled<TargetColorData>( cameraEntity, true );

            foreach( var mass in Query<RefRW<PhysicsMass>>() ) {
                mass.ValueRW = PhysicsMass.CreateKinematic( MassProperties.UnitSphere );
            }

            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

            ecb.Instantiate( animationData.ValueRO.startAnimAudio );

            var levelAudioData = GetSingleton<LevelAudioData>();
            ecb.SetComponentEnabled<FadeOut>( levelAudioData.musicInstance, true );
        }

        if( animationData.ValueRO.timeElapsed < animationData.ValueRO.flareStartTime ) {
            new ContractShapesJob { cameraTransform = cameraTransform, deltaTime = deltaTime, animData = animationData.ValueRO }.ScheduleParallel();
            animationData.ValueRW.moveSpeed += animationData.ValueRO.acceleration * deltaTime;
            animationData.ValueRW.rotSpeed += animationData.ValueRO.angAcceleration * deltaTime;
        }
        else {
            var flareEntity = animationData.ValueRO.flare;
            
            if( !animationData.ValueRO.flared ) {
                // var cameraFwd = cameraTransform.Forward();
                // var cameraUp = cameraTransform.Up();

                SetComponent( flareEntity, new LocalTransform {
                    Position = float3.zero,
                    // Rotation = quaternion.LookRotation( -cameraFwd, cameraUp ),
                    Rotation = cameraTransform.Rotation,
                    Scale = 0f
                } );

                animationData.ValueRW.flared = true;
                
                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

                ecb.Instantiate( animationData.ValueRO.endAnimAudio );
                ecb.DestroyEntity( animatedShapesQuery, EntityQueryCaptureMode.AtPlayback );
            }
            
            var flareTimeElapsed = animationData.ValueRO.timeElapsed - animationData.ValueRO.flareStartTime;
            var flareTransform = GetComponent<LocalTransform>( flareEntity );
            
            if( flareTimeElapsed < animationData.ValueRO.flareDuration ) {
                // scale in the flare
                flareTransform.Scale = animationData.ValueRO.easing.Value.Sample( flareTimeElapsed );
                SetComponent( flareEntity, flareTransform );

                if( !animationData.ValueRO.particlesSpawned ) {
                    var gameStateEntity = GetSingletonEntity<GameStateData>();
                    var gameState = GetComponent<GameStateData>( gameStateEntity );
                    var levelsBuf = GetBuffer<Level>( gameStateEntity );
                    var levelTimer = GetSingleton<LevelTimerData>();
                    
                    // determine number of stars to spawn in
                    var bestTimeThisLevel = levelsBuf[ gameState.curLevelIndex ].bestTime;
                    var numStarsToSpawn = GameStateData.maxNumStars - (int)math.min(levelTimer.value / bestTimeThisLevel, GameStateData.maxNumStars - 1);
                    
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

                    ecb.Instantiate( animationData.ValueRO.particlePrefab, new NativeArray<Entity>( numStarsToSpawn, Allocator.Temp ) );
                    
                    animationData.ValueRW.particlesSpawned = true;
                }
            }
            else {
                var finishedFlareTimeElapsed = flareTimeElapsed - animationData.ValueRO.flareDuration;

                if( finishedFlareTimeElapsed > animationData.ValueRO.postFlareDuration ) {
                    // we're finished with the animation
                    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

                    var animDataEntity = GetSingletonEntity<SuccessAnimData>();
                    ecb.DestroyEntity( animDataEntity );
            
                    var gameStateEntity = GetSingletonEntity<GameStateData>();
                    ecb.AddComponent( gameStateEntity, new SwitchLevel {} );

                    ecb.DestroyEntity( particlesQuery, EntityQueryCaptureMode.AtPlayback );
                }
            }
        }

        animationData.ValueRW.timeElapsed += deltaTime;
    }
    
    [WithAll(typeof(MatchableData))]
    [BurstCompile] partial struct ContractShapesJob : IJobEntity {
        [ReadOnly] public SuccessAnimData animData;
        [ReadOnly] public LocalToWorld cameraTransform;
        [ReadOnly] public float deltaTime;
        
        void Execute( ref LocalTransform transform, in RandomAnimModifierData randomness ) {
            var pcTimeElapsed = animData.timeElapsed / animData.flareStartTime;
            
            var cameraFwd = cameraTransform.Forward;
            var offsetFromCam = transform.Position - cameraTransform.Position;
            var pivot = cameraTransform.Position + math.project( offsetFromCam, cameraFwd );
            var offsetFromPivot = transform.Position - pivot;
            var pivotTransform = new LocalTransform { Position = pivot, Rotation = quaternion.identity, Scale = 1f - pcTimeElapsed };
            var distToPivotSq = math.lengthsq( offsetFromPivot );
            var step = animData.moveSpeed * deltaTime * randomness.value;

            if( distToPivotSq > math.pow(step, 2f) ) {
                transform = transform.Translate( -offsetFromPivot * step );
            }
            
            var curLocalTransform = pivotTransform.TransformTransform( transform );
            
            var rotation = quaternion.AxisAngle( cameraFwd, animData.rotSpeed * deltaTime * randomness.value );
            pivotTransform = pivotTransform.Rotate( rotation );
            transform = pivotTransform.InverseTransformTransform( curLocalTransform );
            
            transform.Scale = 1f - pcTimeElapsed;
        }
    }
}