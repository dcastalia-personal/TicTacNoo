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
public partial struct BuildSpatialMatchesSys : ISystem {
	EntityQuery query;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableData>() );
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		var matchInfoData = GetSingleton<MatchInfoData>();
		var physicsWorld = GetSingleton<PhysicsWorldSingleton>();
		new BuildMatchesSysJob { physicsWorld = physicsWorld, matchInfoData = matchInfoData }.ScheduleParallel();
	}

	[BurstCompile] partial struct BuildMatchesSysJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		[ReadOnly] public MatchInfoData matchInfoData;
		
		void Execute( Entity self, in MatchableData matchableData, ref LocalTransform transform, DynamicBuffer<MatchCandidate> matchCandidates ) {
			matchCandidates.Clear();
			
			var neighborIndices = new NativeList<int>( Allocator.Temp );
			var overlapExtents = new float3( matchInfoData.matchDistance );

			const int matchingMask = 1 << 0;
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
				
				// if( matchableData.debug ) Debug.Log( $"Matchable {self} overlapping with {physicsWorld.Bodies[ hitIndex ].Entity}" );
				
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
				
				foreach( var hitInLine in hitsInLine ) {
					matchCandidates.Add( new MatchCandidate { value = hitInLine.Entity } );
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

[UpdateAfter(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(SetTargetColorByIndexSys))] [UpdateBefore(typeof(LoseLevelSys))]
public partial struct BuildColorMatchesSys : ISystem {
	ComponentLookup<TargetColorData> targetColorDataLookup;
	ComponentLookup<Matched> matchesLookup;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchInfoData>();
		state.RequireForUpdate<InGameData>();
		
		targetColorDataLookup = state.GetComponentLookup<TargetColorData>( isReadOnly: true );
		matchesLookup = state.GetComponentLookup<Matched>( isReadOnly: false );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		matchesLookup.Update( ref state );
		targetColorDataLookup.Update( ref state );

		new BuildColorMatchesSysJob { targetColorDataLookup = targetColorDataLookup, matchInfo = GetSingleton<MatchInfoData>(), matchesLookup = matchesLookup }.ScheduleParallel();
	}

	// [WithChangeFilter(typeof(MatchCandidate))]
	[BurstCompile] partial struct BuildColorMatchesSysJob : IJobEntity {
		[ReadOnly] public ComponentLookup<TargetColorData> targetColorDataLookup;
		[ReadOnly] public MatchInfoData matchInfo;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<Matched> matchesLookup;
		
		void Execute( Entity self, DynamicBuffer<MatchCandidate> matchCandidates ) {
			
			var colorToMatch = targetColorDataLookup[ self ];
			if( colorToMatch.baseColor.Equals( colorToMatch.defaultColor ) ) return;

			// the match candidates are a flattened array of "chains" of candidates, each with a length equal to the critical mass
			for( int chainIndex = 0; chainIndex < matchCandidates.Length; chainIndex += matchInfo.criticalMass ) {

				bool chainMatches = true;
				for( int candidateIndex = 0; candidateIndex < matchInfo.criticalMass; candidateIndex++ ) {
					MatchCandidate matchCandidate = matchCandidates[ chainIndex + candidateIndex ];
					if( matchCandidate.value == self ) continue;

					var candidateColorTarget = targetColorDataLookup[ matchCandidate.value ];
					if( !candidateColorTarget.baseColor.Equals( colorToMatch.baseColor ) ) {
						chainMatches = false;
						break;
					}
				}

				if( !chainMatches ) continue;
				for( int candidateIndex = 0; candidateIndex < matchInfo.criticalMass; candidateIndex++ ) {
					MatchCandidate matchCandidate = matchCandidates[ chainIndex + candidateIndex ];
					matchesLookup.SetComponentEnabled( matchCandidate.value, true );
				}
			}
		}
	}
}

[UpdateBefore(typeof(DestroySys))]
public partial struct LoseLevelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Matched>() );
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		var gameStateData = GetSingleton<GameStateData>();

		SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
		ecb.Instantiate( gameStateData.failureAckPrefab );

		foreach( var targetColorData in Query<RefRW<TargetColorData>>().WithAll<Matched>() ) {
			targetColorData.ValueRW.emission = MatchInfoData.matchedColorIntensity;
		}
	}
}

[UpdateBefore(typeof(SetTargetColorByIndexSys))] [UpdateBefore(typeof(DestroySys))]
public partial struct WinLevelSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var targetColorData in Query<RefRO<TargetColorData>>().WithAll<MatchableData>() ) {
			if( targetColorData.ValueRO.baseColor.Equals( targetColorData.ValueRO.defaultColor ) ) {
				return;
			}
		}

		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		SetComponentEnabled<ShouldDestroy>( GetSingletonEntity<InGameData>(), true );
		ecb.Instantiate( GetSingleton<GameStateData>().successAckPrefab );
	}
}