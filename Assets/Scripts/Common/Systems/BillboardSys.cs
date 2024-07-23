using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

public partial struct BillboardSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BillboardData, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var cameraEntity = GetSingletonEntity<CameraData>();
		
		new BillboardJob { cameraTransform = GetComponent<LocalToWorld>( cameraEntity ) }.ScheduleParallel();
	}

	[WithAll(typeof(BillboardData))]
	[BurstCompile] partial struct BillboardJob : IJobEntity {
		[ReadOnly] public LocalToWorld cameraTransform;
		
		void Execute( ref LocalTransform transform ) {
			var dirToCamera = math.normalize( cameraTransform.Position - transform.Position );
			transform.Rotation = quaternion.LookRotation( -dirToCamera, cameraTransform.Up );
		}
	}
}