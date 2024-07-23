using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InitFailureAckSys : ISystem {
    EntityQuery query;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FailureAckData, RequireInitData>() );
        state.RequireForUpdate( query );
    }

    public void OnUpdate( ref SystemState state ) {
        var ui = GetSingleton<UIData>();
        var gameState = GetSingleton<GameStateData>();
        var gameStateEntity = GetSingletonEntity<GameStateData>();
        var failureAck = GetSingleton<FailureAckData>();
        var failurePanel = failureAck.failureUIPrefab.Value.Instantiate();
        ui.document.Value.rootVisualElement.Add( failurePanel );
		
        var restartButton = failurePanel.Q<Button>( "Restart_Button" );
        var abortButton = failurePanel.Q<Button>( "Abort_Button" );

        var failureAckEntity = GetSingletonEntity<FailureAckData>();
        var failureAnimationPrefab = gameState.failureAnimationPrefab;

        var entityManager = state.EntityManager;
		
        restartButton.clicked += () => {
            // unload current scene and reload
            entityManager.DestroyEntity( failureAckEntity );
            entityManager.Instantiate( failureAnimationPrefab );
            ui.document.Value.rootVisualElement.Remove( failurePanel );
            gameState.nextLevel = gameState.curLevel;
            entityManager.SetComponentData( gameStateEntity, gameState );
        };
		
        abortButton.clicked += () => {
            // unload current scene and load main menu
            entityManager.DestroyEntity( failureAckEntity );
            entityManager.Instantiate( failureAnimationPrefab );
            ui.document.Value.rootVisualElement.Remove( failurePanel );
            gameState.nextLevel = 1; // main menu
            entityManager.SetComponentData( gameStateEntity, gameState );
        };
    }
}

[UpdateAfter(typeof(DestroySys))]
public partial struct TeardownFailureAckSys : ISystem {
    EntityQuery query;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FailureAckData, ShouldDestroy>() );
        state.RequireForUpdate( query );
    }

    public void OnUpdate( ref SystemState state ) {
        if( query.IsEmpty ) return;
        
        var ui = GetSingleton<UIData>();
        ui.document.Value.rootVisualElement.Q<VisualElement>( "Failure_Panel" ).RemoveFromHierarchy();
    }
}

[UpdateBefore(typeof(DestroySys))]
public partial struct FailureAnimationSys : ISystem {

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        state.RequireForUpdate<FailureAnimData>();
    }

    [BurstCompile] public void OnUpdate( ref SystemState state ) {
        var cameraEntity = GetSingletonEntity<CameraData>();
        var cameraTransform = GetComponent<LocalToWorld>( cameraEntity );
        var deltaTime = SystemAPI.Time.DeltaTime;
        var animationData = GetSingletonRW<FailureAnimData>();

        if( animationData.ValueRO.timeElapsed == 0f ) {
            var targetColorData = GetComponent<TargetColorData>( cameraEntity );
            targetColorData.baseColor = targetColorData.defaultColor;
            SetComponent( cameraEntity, targetColorData );
            SetComponentEnabled<TargetColorData>( cameraEntity, true );
            
            foreach( var mass in Query<RefRW<PhysicsMass>>() ) {
                mass.ValueRW = PhysicsMass.CreateKinematic( MassProperties.UnitSphere );
            }
        }

        if( animationData.ValueRO.timeElapsed < animationData.ValueRO.shapesOutDuration ) {
            new FailureAnimationSysJob { cameraTransform = cameraTransform, deltaTime = deltaTime, animationData = animationData.ValueRO }.ScheduleParallel();
            
            animationData.ValueRW.moveSpeed += animationData.ValueRO.acceleration * deltaTime;
            animationData.ValueRW.rotSpeed += animationData.ValueRO.angAcceleration * deltaTime;
        }
        else if( animationData.ValueRO.timeElapsed > animationData.ValueRO.nextLevelStartDuration ) {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
            var gameStateEntity = GetSingletonEntity<GameStateData>();
            ecb.AddComponent( gameStateEntity, new SwitchLevel {} );
            
            SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<FailureAnimData>(), true );
        }

        animationData.ValueRW.timeElapsed += deltaTime;
    }
    
    [WithAll(typeof(MaterialMeshInfo))]
    [BurstCompile] partial struct FailureAnimationSysJob : IJobEntity {
        [ReadOnly] public FailureAnimData animationData;
        [ReadOnly] public LocalToWorld cameraTransform;
        [ReadOnly] public float deltaTime;
        
        void Execute( ref LocalTransform transform, in RandomAnimModifierData randomness ) {
            var cameraFwd = cameraTransform.Forward;
            var offsetFromCam = transform.Position - cameraTransform.Position;
            var pivot = cameraTransform.Position + math.project( offsetFromCam, cameraFwd ) + randomness.randomDir;
            var offsetFromPivot = transform.Position - pivot;
            var pivotTransform = new LocalTransform { Position = pivot, Rotation = quaternion.identity, Scale = 1f };
            transform = transform.Translate( offsetFromPivot * animationData.moveSpeed * deltaTime * randomness.value );
            var curLocalTransform = pivotTransform.TransformTransform( transform );

            var rotation = quaternion.AxisAngle( cameraFwd, animationData.rotSpeed * deltaTime * randomness.value );
            pivotTransform = pivotTransform.Rotate( rotation );
            transform = pivotTransform.InverseTransformTransform( curLocalTransform );

            var pcTimeElapsed = animationData.timeElapsed / animationData.shapesOutDuration;
            transform.Scale = 1f - pcTimeElapsed;
        }
    }
}