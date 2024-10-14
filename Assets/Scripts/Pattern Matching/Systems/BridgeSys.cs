using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using RaycastHit = Unity.Physics.RaycastHit;

public partial struct FindBridgePairsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGamePrefabsData>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithPresent<BridgeData>().WithNone<RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var initEcbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var initEcb = initEcbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		var bridges = query.ToEntityArray( Allocator.TempJob );
		var bridgeBeamPrefab = GetSingleton<InGamePrefabsData>().bridgeBeam;
		
		var job = new FindBridgePairsJob {
			initEcb = initEcb.AsParallelWriter(), 
			bridges = bridges,
			ltwLookup = GetComponentLookup<LocalToWorld>( true ),
			bridgeLookup = GetComponentLookup<BridgeData>( true ),
			colorLookup = GetComponentLookup<MatchableColor>( true ),
			curColorLookup = GetComponentLookup<CurrentColorData>(),
			neutralColor = GetSingleton<GameColorData>().neutral,
			bridgeBeamPrefab = bridgeBeamPrefab,
			beamSettings = GetComponent<BridgeBeamData>( bridgeBeamPrefab ),
		}.ScheduleParallel( query, state.Dependency );

		job.Complete();

		bridges.Dispose();
	}

	[BurstCompile] partial struct FindBridgePairsJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter initEcb;
		[ReadOnly] public NativeArray<Entity> bridges;
		[ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
		[ReadOnly] public ComponentLookup<BridgeData> bridgeLookup;
		[ReadOnly] public ComponentLookup<MatchableColor> colorLookup;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CurrentColorData> curColorLookup;
		public Entity bridgeBeamPrefab;
		public float4 neutralColor;
		public BridgeBeamData beamSettings;
		
		void Execute( Entity self, [ChunkIndexInQuery] int chunkIndex ) {
			var bridge = bridgeLookup[ self ];
			var myColor = colorLookup[ self ];

			if( bridge.beam != Entity.Null ) {
				// if( !curColorLookup[ bridge.beam ].value.Equals( myColor.value ) ) initEcb.SetComponent( chunkIndex, bridge.beam, new CurrentColorData { value = myColor.value } );
				if( !curColorLookup[ bridge.beam ].value.Equals( myColor.value ) ) curColorLookup[ bridge.beam ] = new CurrentColorData { value = myColor.value };
				return;
			}
			
			if( myColor.value.Equals( neutralColor ) ) return;
			
			// of all the Bridge entities, find the closest
			var closest = Entity.Null;
			var closestDist = math.INFINITY;
			var myLtw = ltwLookup[ self ];

			foreach( var otherBridge in bridges ) {
				if( otherBridge == self ) continue;

				var otherBridgeColor = colorLookup[ otherBridge ];
				if( otherBridgeColor.value.Equals( neutralColor ) ) continue;

				if( !myColor.value.Equals( otherBridgeColor.value ) ) continue;

				var otherLtw = ltwLookup[ otherBridge ];
				var distToThisBridge = math.distancesq( myLtw.Position, otherLtw.Position );

				if( distToThisBridge < closestDist ) {
					closest = otherBridge;
					closestDist = distToThisBridge;
				}
			}

			if( closest == Entity.Null ) return;

			beamSettings.start = self;
			beamSettings.end = closest;

			var newBeamEntity = initEcb.Instantiate( chunkIndex, bridgeBeamPrefab );
			initEcb.SetComponent( chunkIndex, newBeamEntity, beamSettings );
			initEcb.SetComponent( chunkIndex, newBeamEntity, new CurrentColorData { value = myColor.value } );
			initEcb.SetComponent( chunkIndex, self, new BridgeData { beam = newBeamEntity } );
		}
	}
}

public partial struct OrientBridgeBeamSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BridgeBeamData, CurrentColorData, LaserLength, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var cameraEntity = GetSingletonEntity<CameraData>();
		var cameraPos = GetComponentRO<LocalToWorld>( cameraEntity );
		new OrientBridgeBeamJob {
			cameraPos = cameraPos.ValueRO.Position, 
			ltwLookup = GetComponentLookup<LocalToWorld>(),
		}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct OrientBridgeBeamJob : IJobEntity {
		public float3 cameraPos;
		[ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
		
		void Execute( in BridgeBeamData bridgeBeamData, in CurrentColorData curColor, ref LaserLength beamLength, ref LocalTransform transform ) {
			var startPos = ltwLookup[ bridgeBeamData.start ].Position;
			var endPos = ltwLookup[ bridgeBeamData.end ].Position;
			var distToTarget = math.distance( startPos, endPos );
			var dirToTarget = math.normalize( endPos - startPos );
			var dirToCamera = cameraPos - startPos;
			beamLength.value = distToTarget;

			transform.Position = startPos;
			transform.Rotation = quaternion.LookRotation( dirToTarget, dirToCamera );
		}
	}
}

[UpdateBefore(typeof(MatchableToTargetColSys))]
public partial struct CheckBetweenBridgeBeamSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BridgeBeamData>() );
		state.RequireForUpdate( query );
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );

		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		if( stepQuery.IsEmpty ) return;

		new CheckBetweenBridgeBeamJob {
			ltwLookup = GetComponentLookup<LocalToWorld>(),
			matchableColorLookup = GetComponentLookup<MatchableColor>(),
			matchableColorChangedLookup = GetComponentLookup<MatchableColorChanged>(),
			physicsWorld = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
		}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct CheckBetweenBridgeBeamJob : IJobEntity {
		[ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<MatchableColor> matchableColorLookup;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<MatchableColorChanged> matchableColorChangedLookup;
		[ReadOnly] public PhysicsWorld physicsWorld;
		
		void Execute( in BridgeBeamData bridgeBeamData ) {
			var startPos = ltwLookup[ bridgeBeamData.start ].Position;
			var endPos = ltwLookup[ bridgeBeamData.end ].Position;

			var hits = new NativeList<RaycastHit>( Allocator.Temp );

			var ray = new RaycastInput { Start = startPos, End = endPos, Filter = bridgeBeamData.collisionFilter };
			if( !physicsWorld.CastRay( ray, ref hits ) ) return;

			foreach( var hit in hits ) {
				if( hit.Entity == bridgeBeamData.start || hit.Entity == bridgeBeamData.end ) continue;
				if( !matchableColorLookup.TryGetComponent( hit.Entity, out MatchableColor otherColor ) ) continue;
				var startColor = matchableColorLookup[ bridgeBeamData.start ];
				startColor.emission = math.max( otherColor.emission, startColor.emission ); // if you're excited, then leave it that way, but if you're neutral, then upgrade
				matchableColorLookup[ hit.Entity ] = startColor;
				matchableColorChangedLookup.SetComponentEnabled( hit.Entity, true );
			}
		}
	}
}

public partial struct DestroyBridgeBeamSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BridgeBeamData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new ClearBridgeBeamJob { ecb = ecb.AsParallelWriter(), matchableColorLookup = GetComponentLookup<MatchableColor>( true ) }.ScheduleParallel( query );
	}

	[BurstCompile] partial struct ClearBridgeBeamJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter ecb;
		[ReadOnly] public ComponentLookup<MatchableColor> matchableColorLookup;
		
		void Execute( [ChunkIndexInQuery] int chunkIndex, Entity self, in BridgeBeamData bridgeBeam ) {
			var startColor = matchableColorLookup[ bridgeBeam.start ];
			var endColor = matchableColorLookup[ bridgeBeam.end ];

			if( startColor.value.Equals( endColor.value ) ) return;

			ecb.DestroyEntity( chunkIndex, self );
			ecb.SetComponent( chunkIndex, bridgeBeam.start, new BridgeData { beam = Entity.Null } );
			ecb.SetComponent( chunkIndex, bridgeBeam.end, new BridgeData { beam = Entity.Null } );
		}
	}
}