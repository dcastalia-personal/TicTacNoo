using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Entities.SystemAPI;

[UpdateBefore(typeof(DestroySys))]
public partial struct StartAnimationSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<StartAnimData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		var animData = GetSingletonRW<StartAnimData>();
		
		foreach( var transform in Query<RefRW<LocalTransform>>().WithAll<CameraData>() ) {
			var nextPos = transform.ValueRO.Position;
			var elapsedTimePc = animData.ValueRO.timeElapsed / animData.ValueRO.duration;
			nextPos.y = math.lerp( animData.ValueRO.startDist, animData.ValueRO.endDist, animData.ValueRO.easing.Value.Sample( elapsedTimePc ) );
			// nextPos.y = math.lerp( animData.ValueRO.startDist, animData.ValueRO.endDist, elapsedTimePc );
			transform.ValueRW.Position = nextPos;
		}
		
		if( animData.ValueRO.timeElapsed == animData.ValueRO.duration ) {
			var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			var animEntity = GetSingletonEntity<StartAnimData>();
			SetComponentEnabled<ShouldDestroy>( animEntity, true );

			if( !animData.ValueRO.startGameAfterCompletion ) return;
			var gameState = GetSingleton<GameStateData>();
			ecb.Instantiate( gameState.inGamePrefab );
			return;
		}

		animData.ValueRW.timeElapsed = math.min( animData.ValueRW.timeElapsed + deltaTime, animData.ValueRO.duration );
	}
}