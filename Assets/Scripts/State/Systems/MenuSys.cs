using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitMenuSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MenuAnimationData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var menuAnimEntity = GetSingletonEntity<MenuAnimationData>();
		var menuAnimData = GetComponent<MenuAnimationData>( menuAnimEntity );
		var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		menuAnimData.musicInstance = ecb.Instantiate( menuAnimData.musicPrefab );
		ecb.SetComponent( menuAnimEntity, menuAnimData );
	}
}

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

		var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		ecb.Instantiate( menuAnimData.flareAudio );
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

		var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.Instantiate( menuData.outAudio );

		state.EntityManager.SetComponentEnabled<FadeOut>( menuData.musicInstance, true );
	}
}

public partial struct StartGameSys : ISystem {
	EntityQuery pressedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadLevelButtonData, Pressed>() );
		state.RequireForUpdate( pressedQuery );
		state.RequireForUpdate<MenuAnimationData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( pressedQuery.IsEmpty ) return;
		
		foreach( var (loadLevelButtonData, self) in Query<RefRO<LoadLevelButtonData>>().WithAll<Pressed>().WithEntityAccess() ) {
			var gameState = GetSingletonRW<GameStateData>();
			gameState.ValueRW.nextLevel = loadLevelButtonData.ValueRO.levelToLoad;
		}
		
		var menuEntity = GetSingletonEntity<MenuAnimationData>();
		state.EntityManager.AddComponent<ExitMenuData>( menuEntity );
	}
}

[UpdateAfter(typeof(StartGameSys))] [UpdateBefore(typeof(ExitGameSys))]
public partial struct StartGameSysManaged : ISystem {
	EntityQuery pressedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ClearSettingsButtonData, Pressed>() );
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

	public void OnUpdate( ref SystemState state ) {
		if( pressedQuery.IsEmpty ) return;
		Application.Quit();
	}
}