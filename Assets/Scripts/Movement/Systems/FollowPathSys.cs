using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitFollowPathSys : ISystem {
	BufferLookup<PathData> pathLookup;
	
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pathLookup = state.GetBufferLookup<PathData>( isReadOnly: true );
		
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowPathData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		pathLookup.Update( ref state );
		
		new InitFollowPathSysJob { pathLookup = pathLookup }.ScheduleParallel();
	}

	[BurstCompile] partial struct InitFollowPathSysJob : IJobEntity {
		[ReadOnly] public BufferLookup<PathData> pathLookup;
		
		void Execute( ref FollowPathData followPathData, in LocalTransform transform ) {
			var path = pathLookup[ followPathData.pathToFollow ];

			var closestWaypointDistSq = math.INFINITY;
			var closestWaypointIndex = -1;
			for( int index = 0; index < path.Length; index++ ) {
				var waypointDistSq = math.distancesq( path[ index ].position, transform.Position );

				if( waypointDistSq < closestWaypointDistSq ) {
					closestWaypointDistSq = waypointDistSq;
					closestWaypointIndex = index;
				}
			}

			followPathData.curWaypointIndex = closestWaypointIndex;
			followPathData.nextWaypointIndex = (closestWaypointIndex + 1) % path.Length;
			followPathData.curWaypointPos = path[ closestWaypointIndex ].position;
			followPathData.nextWaypointPos = path[ followPathData.nextWaypointIndex ].position;
		}
	}
}

public partial struct InitFollowOnStepSys : ISystem {
	EntityQuery query;
	EntityQuery followingQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepped>() );
		followingQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowOnStepData, Following>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		state.EntityManager.SetComponentEnabled<Following>( followingQuery, true );
	}
}

public partial struct FollowOnStepSys : ISystem {
	EntityQuery query;
	ComponentLookup<Following> followingLookup;
	BufferLookup<PathData> pathDataLookup;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		pathDataLookup = state.GetBufferLookup<PathData>( isReadOnly: true );
		followingLookup = state.GetComponentLookup<Following>( isReadOnly: false );
		
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowPathData, FollowOnStepData, Following>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		pathDataLookup.Update( ref state );
		followingLookup.Update( ref state );
		var playerStepEntity = GetSingletonEntity<PlayerStepTag>();
		var playerStep = GetComponent<PlayerStepData>( playerStepEntity );
		
		new FollowOnStepSysJob { followingLookup = followingLookup, pathDataLookup = pathDataLookup, playerStep = playerStep }.ScheduleParallel();
	}

	[WithAll( typeof(Following), typeof(FollowPathData), typeof(FollowOnStepData) )] [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	[BurstCompile] partial struct FollowOnStepSysJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<Following> followingLookup;
		[ReadOnly] public BufferLookup<PathData> pathDataLookup;
		[ReadOnly] public PlayerStepData playerStep;
		
		void Execute( Entity self, ref FollowPathData followPathData, in FollowOnStepData followOnStepData, EnabledRefRW<Following> followingEnabled, ref LocalTransform transform ) {

			var normalizedStepTime = playerStep.time / playerStep.duration;
			transform.Position = math.lerp( followPathData.curWaypointPos, followPathData.nextWaypointPos, normalizedStepTime );

			if( normalizedStepTime < 1f ) return;

			followingEnabled.ValueRW = false;
			
			var path = pathDataLookup[ followPathData.pathToFollow ];
			followPathData.curWaypointIndex = followPathData.nextWaypointIndex;
			followPathData.nextWaypointIndex = (followPathData.nextWaypointIndex + 1) % path.Length;
			followPathData.curWaypointPos = path[ followPathData.curWaypointIndex ].position;
			followPathData.nextWaypointPos = path[ followPathData.nextWaypointIndex ].position;
		}
	}
}