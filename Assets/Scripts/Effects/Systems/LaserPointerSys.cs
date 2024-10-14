using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using Random = Unity.Mathematics.Random;

public partial struct SpawnLaserPointerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Pressed, BeamTargetData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (beamTargetData, self) in Query<RefRO<BeamTargetData>>().WithAll<Pressed>().WithEntityAccess() ) {
			var colorDirectory = GetSingletonBuffer<LevelColorElem>();
			var colorIndex = GetSingletonRW<LevelColorIndex>();
			
			var oldColorIndex = colorIndex.ValueRO.current;
			
			// create laser pointer of the appropriate color
			var input = GetSingleton<Input>();
			var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
			var laserPointerPrefabData = GetComponent<LaserPointerData>( input.laserPointerPrefab );
			laserPointerPrefabData.target = self;
			laserPointerPrefabData.audioLoopInstance = ecb.Instantiate( laserPointerPrefabData.audioLoopPrefab );
			laserPointerPrefabData.audioInInstance = ecb.Instantiate( laserPointerPrefabData.audioInPrefab );
			
			var laserPtrInstance = ecb.Instantiate( input.laserPointerPrefab );
			ecb.SetComponent( laserPtrInstance, new CurrentColorData { value = colorDirectory[ oldColorIndex ].color } );
			ecb.SetComponent( laserPtrInstance, laserPointerPrefabData );
		}
	}
}

[UpdateInGroup((typeof(InitializationSystemGroup)))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitLaserPointerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LaserPointerData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		
		foreach( var (laserPointerData, color, self) 
		        in Query<RefRO<LaserPointerData>, RefRO<CurrentColorData>>().WithAll<RequireInitData>().WithEntityAccess() ) {

			var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			var rng = new Random( (uint)SystemAPI.Time.ElapsedTime );

			var impact = ecb.Instantiate( laserPointerData.ValueRO.beamImpactPrefab );
			
			var matchTargetData = GetComponent<MatchTargetPosData>( laserPointerData.ValueRO.beamImpactPrefab );
			matchTargetData.target = self;
			
			ecb.SetComponent( impact, new RandomOffsetData { value = rng.NextFloat( 0f, 1f ) } );
			ecb.SetComponent( impact, color.ValueRO );
			ecb.SetComponent( impact, matchTargetData );
			ecb.AppendToBuffer( self, new LinkedEntityGroup { Value = impact } );

			var jitterData = GetComponent<JitterData>( laserPointerData.ValueRO.target );
			jitterData.acceleration = math.abs( jitterData.acceleration );
			ecb.SetComponent( laserPointerData.ValueRO.target, jitterData );
			ecb.SetComponentEnabled<JitterData>( laserPointerData.ValueRO.target, true );
		}
	}
}

[UpdateAfter(typeof(CollectInput))] [UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlaceLaserPointerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LaserPointerData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {

		var camEntity = GetSingletonEntity<CameraData>();
		var camData = GetComponent<CameraData>( camEntity );
		var camLTW = GetComponent<LocalToWorld>( camEntity );

		foreach( var (transform, laserLength, laserData, self) in Query<RefRW<LocalTransform>, RefRW<LaserLength>, RefRO<LaserPointerData>>().WithEntityAccess() ) {

			var heldData = GetComponent<Held>( laserData.ValueRO.target );
			
			var heldPosLocalToCam = math.transform( math.inverse( camLTW.Value ), heldData.worldPos );
			heldPosLocalToCam.z = 0f;
			var heldDirLocalToCam = math.normalize( heldPosLocalToCam );
			var worldDir = math.normalize( math.transform( camLTW.Value, heldDirLocalToCam ) - camLTW.Position );
			var endPos = camLTW.Position + worldDir * (camData.orthoSize + 0.5f) * camData.aspect;
			transform.ValueRW.Position = heldData.worldPos;

			var offsetStartToEnd = endPos - transform.ValueRO.Position;
			var dirStartToEnd = math.normalize( offsetStartToEnd );
			var lengthStartToHeld = math.length( offsetStartToEnd );
			transform.ValueRW.Rotation = quaternion.LookRotation( dirStartToEnd, -worldDir );
			laserLength.ValueRW.value = lengthStartToHeld;

			SetComponent( laserData.ValueRO.audioLoopInstance, transform.ValueRO );
			if( state.EntityManager.Exists( laserData.ValueRO.audioInInstance ) ) SetComponent( laserData.ValueRO.audioInInstance, transform.ValueRO );
		}
	}
}

[UpdateAfter(typeof(CollectInput))]
public partial struct DestroyLaserPointerSys : ISystem {
	EntityQuery query;
	EntityQuery releasedQuery;
	EntityQuery lostTargetQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LaserPointerData>() );
		state.RequireForUpdate( query );

		releasedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PointerReleased>() );
		lostTargetQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PointerHeld>().WithNone<PointerTarget>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( releasedQuery.IsEmpty && lostTargetQuery.IsEmpty ) return;

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		var laserEntity = query.GetSingletonEntity();
		var laserData = GetComponent<LaserPointerData>( laserEntity );
		var laserTransform = GetComponent<LocalTransform>( laserEntity );
		ecb.DestroyEntity( laserEntity );
		ecb.SetComponentEnabled<FadeOut>( laserData.audioLoopInstance, true );
		var audioOut = ecb.Instantiate( laserData.audioOutPrefab );
		ecb.SetComponent( audioOut, laserTransform );

		if( !IsComponentEnabled<Matched>( laserData.target ) ) {
			var jitterData = GetComponent<JitterData>( laserData.target );
			jitterData.acceleration *= -1f;
			ecb.SetComponent( laserData.target, jitterData );
		}
	}
}