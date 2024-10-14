using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

public partial struct LookAtCameraSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LookAtCameraData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var cameraEntity = GetSingletonEntity<CameraData>();
		var cameraLTW = GetComponent<LocalToWorld>( cameraEntity );
		
		new LookAtCameraJob { cameraLTW = cameraLTW }.ScheduleParallel();
	}

	[BurstCompile] partial struct LookAtCameraJob : IJobEntity {
		[ReadOnly] public LocalToWorld cameraLTW;
		
		void Execute( in LookAtCameraData lookAtCameraData, ref LocalTransform transform ) {
			var dirToCamera = math.normalize( cameraLTW.Position - transform.Position );
			transform.Rotation = quaternion.LookRotation( dirToCamera, cameraLTW.Up );
		}
	}
}