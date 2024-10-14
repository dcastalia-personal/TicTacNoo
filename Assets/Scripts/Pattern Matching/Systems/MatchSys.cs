using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using RaycastHit = Unity.Physics.RaycastHit;

// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // I feel like this should be in the fixed step simulation group, but it really impacts performance to put it there
// [UpdateAfter(typeof(PhysicsSystemGroup))]
[UpdateBefore((typeof(ClearFinishedPlayerSteppingSys)))]
public partial struct BuildSpatialMatchesSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;
	EntityQuery onlyMatchOnPlayerStepQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableData>() );
		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAny<PlayerFinishedStepping, PlayerStepped>() );
		state.RequireForUpdate( stepQuery );
		
		onlyMatchOnPlayerStepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<OnlyMatchOnPlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		if( !onlyMatchOnPlayerStepQuery.IsEmpty && stepQuery.IsEmpty ) return;
		
		var matchInfoData = GetSingleton<MatchInfoData>();
		var physicsWorld = GetSingleton<PhysicsWorldSingleton>();
		new BuildMatchesSysJob {
			physicsWorld = physicsWorld, 
			matchInfoData = matchInfoData, 
			matchableColorLookup = GetComponentLookup<MatchableColor>( true ), 
			matchesLookup = GetComponentLookup<Matched>(),
			neutralColor = GetSingleton<GameColorData>().neutral,
		}.ScheduleParallel();
	}

	[BurstCompile] partial struct BuildMatchesSysJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		[ReadOnly] public MatchInfoData matchInfoData;
		[ReadOnly] public ComponentLookup<MatchableColor> matchableColorLookup;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<Matched> matchesLookup;
		public float4 neutralColor;
		
		void Execute( Entity self, in MatchableData matchableData, ref LocalTransform transform ) {
			var myColor = matchableColorLookup[ self ];
			if( myColor.value.Equals( neutralColor ) ) return;
			
			var neighborIndices = new NativeList<int>( Allocator.Temp );
			var overlapExtents = new float3( matchInfoData.matchDistance );

			const uint matchingMask = 1 << 0;
			if( !physicsWorld.OverlapAabb(
				   new OverlapAabbInput {
					   Aabb = new Aabb { Max = transform.Position + overlapExtents, Min = transform.Position - overlapExtents },
					   Filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask }
				   },
				   ref neighborIndices ) ) {
				return;
			}

			foreach( var hitIndex in neighborIndices ) {
				if( physicsWorld.Bodies[ hitIndex ].Entity == self ) continue;
				
				var neighborBody = physicsWorld.Bodies[ hitIndex ];
				var dirToNeighbor = math.normalize( neighborBody.WorldFromBody.pos - transform.Position );

				var hitsInLine = new NativeList<RaycastHit>( Allocator.Temp );

				var raycastThroughNeighbor = new RaycastInput {
					Start = transform.Position, 
					End = transform.Position + dirToNeighbor * matchInfoData.matchDistance, // constant here allows for diagonal matching 
					Filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask }
				};

				physicsWorld.CastRay( raycastThroughNeighbor, ref hitsInLine );
				if( hitsInLine.Length < matchInfoData.criticalMass ) continue;
				
				// sort by distance before resizing
				hitsInLine.Sort( new HitDistanceComparer() );
				hitsInLine.Resize( matchInfoData.criticalMass, NativeArrayOptions.UninitializedMemory ); // if you hit more targets than the critical mass, it's only the critical mass that we care about

				// Test for the same color
				bool allColorsTheSame = true;

				foreach( var hitInLine in hitsInLine ) {
					if( hitInLine.Entity == self ) continue;
					var otherColor = matchableColorLookup[ hitInLine.Entity ];
					if( otherColor.value.Equals( myColor.value ) ) continue;
					
					allColorsTheSame = false;
					break;
				}

				if( !allColorsTheSame ) continue;

				// Test for obstacles
				const uint obstaclesMask = 1 << 1;
				var raycastThroughObstacles = new RaycastInput {
					Start = transform.Position, 
					End = hitsInLine[^1].Position, // constant here allows for diagonal matching 
					Filter = new CollisionFilter { BelongsTo = obstaclesMask, CollidesWith = obstaclesMask }
				};

				var obstaclesInLine = new NativeList<RaycastHit>( Allocator.Temp );
				physicsWorld.CastRay( raycastThroughObstacles, ref obstaclesInLine );

				bool encounteredObstacle = false;
				foreach( var obstacle in obstaclesInLine ) {
					if( obstacle.Entity == self ) continue;
					var obstacleCol = matchableColorLookup[ obstacle.Entity ];
					if( obstacleCol.value.Equals( myColor.value ) ) continue;
					encounteredObstacle = true;
				}

				if( encounteredObstacle ) continue;

				foreach( var hit in hitsInLine ) {
					matchesLookup.SetComponentEnabled( hit.Entity, true );
				}
			}
		}
	}
}

[BurstCompile] struct HitDistanceComparer : IComparer<RaycastHit> {
 
	[BurstCompile] public int Compare(RaycastHit a, RaycastHit b) {
		if( a.Fraction < b.Fraction ) return -1;
		if( a.Fraction == b.Fraction ) return 0;
		if( a.Fraction > b.Fraction ) return 1;

		return 0;
	}
}

[UpdateBefore(typeof(DestroySys))] [UpdateBefore(typeof(UpdatePlayerStepSys))]
public partial struct LoseLevelSys : ISystem {
	EntityQuery query;
	EntityQuery winQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Matched>() );
		state.RequireForUpdate<InGameData>();
		
		winQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SuccessAckData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		var gameStateData = GetSingleton<GameStateData>();

		SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
		ecb.Instantiate( gameStateData.failureAckPrefab );
		
		// if we have won previously, then un-win; this is just a contingency for odd timings because system order should normally prevent this from happening
		ecb.DestroyEntity( winQuery, EntityQueryCaptureMode.AtPlayback );
		
		// freeze step time if it's moving
		foreach( var playerStepData in Query<RefRW<PlayerStepData>>() ) {
			playerStepData.ValueRW.time = playerStepData.ValueRO.duration;
		}

		foreach( var (targetColorData, matchableColor, self) in Query<RefRW<TargetColorData>, RefRW<MatchableColor>>().WithAll<Matched>().WithEntityAccess() ) {
			targetColorData.ValueRW.emission = MatchInfoData.matchedColorIntensity;
			matchableColor.ValueRW.emission = MatchInfoData.matchedColorIntensity;
			
			SetComponentEnabled<JitterData>( self, true );
		}
		
		foreach( var (mass, velocity) in Query<RefRW<PhysicsMass>, RefRW<PhysicsVelocity>>().WithAll<VelocityRespondsToPlayerStepData>() ) {
			mass.ValueRW.InverseInertia = float3.zero;
			mass.ValueRW.InverseMass = 0f;

			velocity.ValueRW = new PhysicsVelocity();
		}
	}
}

[UpdateBefore(typeof(SetMatchableColorByIndexSys))] [UpdateBefore(typeof(DestroySys))] [UpdateBefore(typeof(UpdatePlayerStepSys))]
public partial struct WinLevelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var targetColorData in Query<RefRO<TargetColorData>>().WithAll<MatchableData>() ) {
			if( targetColorData.ValueRO.baseColor.Equals( GetSingleton<GameColorData>().neutral ) ) {
				return;
			}
		}

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
		ecb.Instantiate( GetSingleton<GameStateData>().successAckPrefab );
		
		// freeze step time if it's moving
		foreach( var playerStepData in Query<RefRW<PlayerStepData>>() ) {
			playerStepData.ValueRW.time = playerStepData.ValueRO.duration;
		}
	}
}