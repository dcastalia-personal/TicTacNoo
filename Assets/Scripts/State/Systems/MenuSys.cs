using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateAfter(typeof(FollowSplineContinuouslySys))] [UpdateBefore(typeof(ClearFinishedFollowingSys))]
public partial struct RefreshFlareSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FinishedFollowing>() );

		state.RequireForUpdate<MenuAnimationData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		var menuAnimData = GetSingleton<MenuAnimationData>();
		var existingFlareData = GetComponent<ScaleOnCurveData>( menuAnimData.flare );
		existingFlareData.elapsedTime = 0f;
		SetComponent( menuAnimData.flare, existingFlareData );
		SetComponentEnabled<ScaleOnCurveData>( menuAnimData.flare, true );
	}
}

public partial struct ExitMenuSys : ISystem {
	EntityQuery query;
	EntityQuery followingQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ExitMenuData>() );
		state.RequireForUpdate( query );
		
		followingQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowContinuouslyData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var menuEntity = GetSingletonEntity<MenuAnimationData>();
		var menuData = GetComponent<MenuAnimationData>( menuEntity );

		if( TryGetSingletonEntity<SpawnAtIntervalData>( out Entity spawner ) ) {
			state.EntityManager.RemoveComponent<SpawnAtIntervalData>( spawner );
		}
		
		state.EntityManager.RemoveComponent<FollowContinuouslyData>( followingQuery ); // stop all the following objects from moving so that the end animation can take over
		state.EntityManager.DestroyEntity( menuEntity );
		state.EntityManager.Instantiate( menuData.outTransitionPrefab );
	}
}

public partial struct StartGameSys : ISystem {
	EntityQuery pressedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StartGameButtonData, Pressed>() );
		state.RequireForUpdate( pressedQuery );
		state.RequireForUpdate<MenuAnimationData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( pressedQuery.IsEmpty ) return;
		
		var gameState = GetSingletonRW<GameStateData>();
		gameState.ValueRW.nextLevel = 2;

		var menuEntity = GetSingletonEntity<MenuAnimationData>();
		state.EntityManager.AddComponent<ExitMenuData>( menuEntity );
	}
}

[UpdateAfter(typeof(StartGameSys))] [UpdateBefore(typeof(ExitGameSys))]
public partial struct StartGameSysManaged : ISystem {
	EntityQuery pressedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StartGameButtonData, Pressed>() );
		state.RequireForUpdate( pressedQuery );
		state.RequireForUpdate<MenuAnimationData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( pressedQuery.IsEmpty ) return;

		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
	}
}

public partial struct ExitGameSys : ISystem {
	EntityQuery pressedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ExitGameButtonData, Pressed>() );
		state.RequireForUpdate( pressedQuery );
		state.RequireForUpdate<MenuAnimationData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( pressedQuery.IsEmpty ) return;
		
		var gameState = GetSingletonRW<GameStateData>();
		gameState.ValueRW.nextLevel = -1;

		var menuEntity = GetSingletonEntity<MenuAnimationData>();
		state.EntityManager.AddComponent<ExitMenuData>( menuEntity );
	}
}