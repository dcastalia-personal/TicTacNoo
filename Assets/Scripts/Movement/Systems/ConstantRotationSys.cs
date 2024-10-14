using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateBefore(typeof(InitPrevVelSys))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct ConstantRotationSys : ISystem {
	EntityQuery query;
	EntityQuery playerSteppedQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ConstantRotationData, LocalTransform>() );
		state.RequireForUpdate( query );

		playerSteppedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( playerSteppedQuery.IsEmpty ) return;
		new ConstantRotationJob { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel( query );
	}

	[BurstCompile] partial struct ConstantRotationJob : IJobEntity {
		public float deltaTime;
		
		void Execute( in ConstantRotationData constantRot, ref LocalTransform transform ) {
			transform = transform.Rotate( quaternion.Euler( constantRot.rotPerSecond * deltaTime ) );
		}
	}
}