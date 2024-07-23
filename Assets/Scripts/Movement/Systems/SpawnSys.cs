using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

public partial struct SpawnSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SpawnAtIntervalData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		var deltaTime = SystemAPI.Time.DeltaTime;
		
		foreach( var (spawnAtIntervalData, transform, self) in Query<RefRW<SpawnAtIntervalData>, RefRO<LocalTransform>>().WithEntityAccess() ) {
			spawnAtIntervalData.ValueRW.time += deltaTime;
			
			if( spawnAtIntervalData.ValueRO.time > spawnAtIntervalData.ValueRO.interval ) {
				var instance = ecb.Instantiate( spawnAtIntervalData.ValueRO.prefabToSpawn );
				ecb.SetComponent( instance, transform.ValueRO );
				ecb.AddComponent<RequireInitData>( instance );
				
				spawnAtIntervalData.ValueRW.time = 0f;
				spawnAtIntervalData.ValueRW.curSpawnCount++;

				if( spawnAtIntervalData.ValueRO.curSpawnCount >= spawnAtIntervalData.ValueRO.maxSpawnCount ) {
					ecb.DestroyEntity( self );
				}
			}
		}
	}
}