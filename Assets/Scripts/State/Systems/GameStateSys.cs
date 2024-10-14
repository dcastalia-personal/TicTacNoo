using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitGameStateSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	#if !UNITY_EDITOR 
	[BurstCompile]
	#endif
	public void OnUpdate( ref SystemState state ) {
		var gameStateData = GetSingletonRW<GameStateData>();
		var gameStateEntity = GetSingletonEntity<GameStateData>();
		
		// load first scene on start
		var levelToLoad = gameStateData.ValueRO.startLevel;
		#if UNITY_EDITOR
		if( LoadCommonIfMissing.overrideStartSceneIndex != -1 ) levelToLoad = LoadCommonIfMissing.overrideStartSceneIndex;
		#endif
		gameStateData.ValueRW.nextLevel = levelToLoad;
		state.EntityManager.AddComponent<SwitchLevel>( gameStateEntity );
	}
}

public partial struct SwitchLevelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SwitchLevel>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var gameStateEntity = GetSingletonEntity<GameStateData>();
		var gameState = GetComponent<GameStateData>( gameStateEntity );
		var levels = GetSingletonBuffer<Level>();
		var nextLevelIndex = gameState.nextLevel;

		var nextLevel = levels[ nextLevelIndex ];

		if( gameState.curLoadedScene != Entity.Null ) {
			SceneSystem.UnloadScene( state.WorldUnmanaged, gameState.curLoadedScene, SceneSystem.UnloadParameters.DestroyMetaEntities );
		}
		
		var levelToLoad = SceneSystem.LoadSceneAsync( state.WorldUnmanaged, nextLevel.reference, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
		
		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.RemoveComponent<SwitchLevel>( query, EntityQueryCaptureMode.AtPlayback );

		gameState.curLevelIndex = nextLevelIndex;
		gameState.curLoadedScene = levelToLoad;

		SetComponent( gameStateEntity, gameState );
		SetComponentEnabled<VictoryEnabled>( gameStateEntity, false );
	}
}

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitPrevVelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PhysicsVelocity, PhysicsMass, VelocityRespondsToPlayerStepData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitPrevVelJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct InitPrevVelJob : IJobEntity {
		void Execute( ref PhysicsVelocity physicsVelocity, ref PhysicsMass mass, ref VelocityRespondsToPlayerStepData prevVel ) {
			// remember your initial velocity, in case you start the game moving
			prevVel.linearVelocity = physicsVelocity.Linear;
			prevVel.angularVelocity = physicsVelocity.Angular;
			prevVel.inverseInertia = mass.InverseInertia;
			prevVel.inverseMass = mass.InverseMass;
			
			// start everything "paused" even if it has initial velocity
			physicsVelocity.Linear = float3.zero;
			physicsVelocity.Angular = float3.zero;
		}
	}
}

public partial struct ClearFinishedPlayerSteppingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerFinishedStepping>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var playerFinishedStepping in Query<EnabledRefRW<PlayerFinishedStepping>>() ) {
			playerFinishedStepping.ValueRW = false;
		}
	}
}

[UpdateAfter(typeof(ClearFinishedPlayerSteppingSys))]
public partial struct ClearPlayerStepSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepped>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		foreach( var (playerStepData, playerStepDataEnabled, playerSteppedEvtEnabled) in Query<
			        RefRW<PlayerStepData>, EnabledRefRW<PlayerStepData>, EnabledRefRW<PlayerStepped>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			playerStepData.ValueRW.time = 0f;
			playerStepDataEnabled.ValueRW = true;
			playerSteppedEvtEnabled.ValueRW = false;
		}
		
		foreach( var (velocity, mass, prevVelocity, self) in Query<RefRW<PhysicsVelocity>, RefRW<PhysicsMass>, RefRO<VelocityRespondsToPlayerStepData>>().WithEntityAccess() ) {
			velocity.ValueRW.Linear = prevVelocity.ValueRO.linearVelocity;
			velocity.ValueRW.Angular = prevVelocity.ValueRO.angularVelocity;

			mass.ValueRW.InverseMass = prevVelocity.ValueRO.inverseMass;
			mass.ValueRW.InverseInertia = prevVelocity.ValueRO.inverseInertia;
		}
	}
}

[UpdateAfter(typeof(ClearPlayerStepSys))]
public partial struct UpdatePlayerStepSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		var deltaTime = SystemAPI.Time.DeltaTime;

		foreach( var (playerStepData, self) in Query<RefRW<PlayerStepData>>().WithEntityAccess() ) {
			playerStepData.ValueRW.time += deltaTime;

			if( playerStepData.ValueRO.time >= playerStepData.ValueRO.duration ) {
				playerStepData.ValueRW.time = playerStepData.ValueRO.duration;
				SetComponentEnabled<PlayerStepData>( self, false );
				
				foreach( var (velocity, mass, prevVelocity) in Query<RefRW<PhysicsVelocity>, RefRW<PhysicsMass>, RefRW<VelocityRespondsToPlayerStepData>>() ) {
					prevVelocity.ValueRW.linearVelocity = velocity.ValueRO.Linear;
					prevVelocity.ValueRW.angularVelocity = velocity.ValueRO.Angular;
					prevVelocity.ValueRW.inverseInertia = mass.ValueRO.InverseInertia;
					prevVelocity.ValueRW.inverseMass = mass.ValueRO.InverseMass;

					velocity.ValueRW = new PhysicsVelocity();
					mass.ValueRW.InverseMass = 0f;
					mass.ValueRW.InverseInertia = float3.zero;
				}

				SetComponentEnabled<PlayerFinishedStepping>( self, true );
			}
		}
	}
}

[UpdateBefore(typeof(PauseGameSys))]
public partial struct EscapeFromGameSys : ISystem {
	public const string escapeInput = "Exit";
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGameData>();
	}

	public void OnUpdate( ref SystemState state ) {
		
		if( InputSystem.actions[escapeInput].WasPressedThisFrame() ) {
			var gameStateEntity = GetSingletonEntity<GameStateData>();
			state.EntityManager.AddComponent<PauseGameTag>( gameStateEntity );
		}
	}
}

[UpdateBefore(typeof(DestroySys))]
public partial struct PauseGameSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, PauseGameTag>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		
		foreach( var (gameStateData, self) in Query<RefRO<GameStateData>>().WithAll<PauseGameTag>().WithEntityAccess() ) {
			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
			ecb.Instantiate( gameStateData.ValueRO.failureAckPrefab );
		}

		ecb.RemoveComponent<PauseGameTag>( query, EntityQueryCaptureMode.AtPlayback );
	}
}

// public partial struct DebugWinLoseSys : ISystem {
// 	
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		state.RequireForUpdate<InGameData>();
// 	}
//
// 	public void OnUpdate( ref SystemState state ) {
// 		
// 		if( Keyboard.current.wKey.wasPressedThisFrame ) {
// 			var gameStateData = GetSingleton<GameStateData>();
//
// 			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
// 			state.EntityManager.Instantiate( gameStateData.successAckPrefab );
// 		}
// 		
// 		if( Keyboard.current.lKey.wasPressedThisFrame ) {
// 			var gameStateData = GetSingleton<GameStateData>();
//
// 			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
// 			state.EntityManager.Instantiate( gameStateData.failureAckPrefab );
// 		}
// 	}
// }

[UpdateBefore(typeof(DestroySys))]
public partial struct UnescapeFromGameSys : ISystem {
	EntityQuery query;
	EntityQuery matchedData;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FailureAckData>() );
		state.RequireForUpdate( query );
		
		matchedData = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Matched>() );
	}

	public void OnUpdate( ref SystemState state ) {
		if( !matchedData.IsEmpty ) return; // only allow returning to the game if there is no match (i.e. you paused the game with the escape key)

		if( InputSystem.actions[EscapeFromGameSys.escapeInput].WasPressedThisFrame() ) {
			var gameState = GetSingleton<GameStateData>();

			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<FailureAckData>(), true );
			ecb.Instantiate( gameState.inGamePrefab );
		}
	}
}