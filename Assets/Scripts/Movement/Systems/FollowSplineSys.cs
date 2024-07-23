using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Splines;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InitFollowSplineSys : ISystem {
	EntityQuery query;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowSplineData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitFollowSplineSysJob {}.ScheduleParallel();
	}

	[WithAll(typeof(RequireInitData))]
	[BurstCompile] partial struct InitFollowSplineSysJob : IJobEntity {
		// set initial position on spline
		void Execute( ref FollowSplineData followSplineData, ref LocalTransform transform ) {
			var spline = followSplineData.spline.Value.ToNativeArray();
			spline.GetClosestPt( transform.Position, out float3 closestPos, out int closestPtIndex );
			var distFromStart = spline.Distance( closestPtIndex );
			transform.Position = closestPos;

			followSplineData.timeElapsed = distFromStart / followSplineData.spline.Value.distance;
		}
	}
}

public partial struct FollowSplineContinuouslySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowSplineData, FollowContinuouslyData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var deltaTime = SystemAPI.Time.DeltaTime; 
		new FollowSplineSysJob { deltaTime = deltaTime }.ScheduleParallel();
	}

	[WithAll(typeof(FollowContinuouslyData))] [WithNone(typeof(RequireInitData))] [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	[BurstCompile] partial struct FollowSplineSysJob : IJobEntity {
		[ReadOnly] public float deltaTime;
		
		void Execute( ref FollowSplineData followSplineData, ref LocalTransform transform, EnabledRefRW<FinishedFollowing> finished ) {
			var spline = followSplineData.spline.Value.ToNativeArray();
			spline.Sample( followSplineData.timeElapsed, out transform.Position );
			
			followSplineData.timeElapsed += deltaTime * followSplineData.speed;

			if( followSplineData.timeElapsed > 1f ) {
				finished.ValueRW = true;
				followSplineData.timeElapsed %= 1f;
			}
		}
	}
}

[UpdateAfter(typeof(FollowSplineContinuouslySys))]
public partial struct ClearFinishedFollowingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FollowSplineData, FinishedFollowing>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		new ClearFinishedFollowingJob {}.ScheduleParallel();
	}

	[WithAll(typeof(FollowSplineData))]
	[BurstCompile] partial struct ClearFinishedFollowingJob : IJobEntity {
		void Execute( EnabledRefRW<FinishedFollowing> finished ) {
			finished.ValueRW = false;
		}
	}
}