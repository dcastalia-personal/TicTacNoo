using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

public partial struct InitGameStateSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var gameStateData = GetSingletonRW<GameStateData>();
		
		if( SceneManager.GetActiveScene().buildIndex != 0 ) {
			gameStateData.ValueRW.curLevel = SceneManager.GetActiveScene().buildIndex;
			return; // loaded into a scene where LevelStateData already exists
		}
		
		// load main menu on start
		SceneManager.LoadSceneAsync( gameStateData.ValueRO.startLevel, LoadSceneMode.Additive );
		gameStateData.ValueRW.curLevel = gameStateData.ValueRO.startLevel;
	}
}

public partial struct SwitchLevelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SwitchLevel>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var gameState = GetSingletonRW<GameStateData>();

		if( gameState.ValueRO.nextLevel == -1 ) {
			Application.Quit();
			return;
		}
		
		SceneManager.UnloadSceneAsync( gameState.ValueRO.curLevel );
		SceneManager.LoadSceneAsync( gameState.ValueRO.nextLevel, LoadSceneMode.Additive );
		gameState.ValueRW.curLevel = gameState.ValueRO.nextLevel;

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.RemoveComponent<SwitchLevel>( query, EntityQueryCaptureMode.AtPlayback );
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
		
		foreach( var (playerStepData, playerStepDataEnabled, playerSteppedEvtEnabled, playerFinishedStepping) in Query<
			        RefRW<PlayerStepData>, EnabledRefRW<PlayerStepData>, EnabledRefRW<PlayerStepped>, EnabledRefRW<PlayerFinishedStepping>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			playerStepData.ValueRW.time = 0f;
			playerStepDataEnabled.ValueRW = true;
			playerSteppedEvtEnabled.ValueRW = false;
			playerFinishedStepping.ValueRW = true;
		}
		
		foreach( var (velocity, prevVelocity) in Query<RefRW<PhysicsVelocity>, RefRO<VelocityRespondsToPlayerStepData>>() ) {
			velocity.ValueRW.Linear = prevVelocity.ValueRO.linearVelocity;
			velocity.ValueRW.Angular = prevVelocity.ValueRO.angularVelocity;
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
				
				foreach( var (velocity, prevVelocity) in Query<RefRW<PhysicsVelocity>, RefRW<VelocityRespondsToPlayerStepData>>() ) {
					prevVelocity.ValueRW.linearVelocity = velocity.ValueRO.Linear;
					prevVelocity.ValueRW.angularVelocity = velocity.ValueRO.Angular;
					
					velocity.ValueRW.Linear = float3.zero;
					velocity.ValueRW.Angular = float3.zero;
				}
			}
		}
	}
}

[UpdateBefore(typeof(DestroySys))]
public partial struct EscapeFromGameSys : ISystem {
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGameData>();
	}

	public void OnUpdate( ref SystemState state ) {
		
		if( Keyboard.current.escapeKey.wasPressedThisFrame ) {
			var gameState = GetSingleton<GameStateData>();
			
			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
			ecb.Instantiate( gameState.failureAckPrefab );
		}
	}
}

#if UNITY_EDITOR
[UpdateBefore(typeof(DestroySys))]
public partial struct DebugAdvanceLevelSys : ISystem {

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGameData>();
	}

	public void OnUpdate( ref SystemState state ) {
		
		if( Keyboard.current.wKey.wasPressedThisFrame ) {
			Debug.Log( $"Win!" );
			var gameState = GetSingleton<GameStateData>();
			
			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
			ecb.Instantiate( gameState.successAckPrefab );
		}
		if( Keyboard.current.lKey.wasPressedThisFrame ) {
			var gameState = GetSingleton<GameStateData>();
			
			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
			ecb.Instantiate( gameState.failureAckPrefab );
		}
	}
}
#endif

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

		if( Keyboard.current.escapeKey.wasPressedThisFrame ) {
			var gameState = GetSingleton<GameStateData>();

			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
			SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<FailureAckData>(), true );
			ecb.Instantiate( gameState.inGamePrefab );
		}
	}
}