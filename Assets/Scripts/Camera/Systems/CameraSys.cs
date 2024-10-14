using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitCameraRigSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<CameraData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {

		foreach( var (cameraData, targetColorData, transform ) 
		        in Query<RefRW<CameraData>, RefRW<TargetColorData>, RefRW<LocalToWorld>>().WithOptions( EntityQueryOptions.IgnoreComponentEnabledState ) ) {
			var mainCamera = Camera.main;
			cameraData.ValueRW.camera.Value = mainCamera;
			cameraData.ValueRW.aspect = mainCamera.aspect;
			mainCamera.orthographic = cameraData.ValueRO.orthographic;
			mainCamera.orthographicSize = cameraData.ValueRO.orthoSize;
			mainCamera.transform.position = transform.ValueRO.Position;
			mainCamera.transform.rotation = transform.ValueRO.Rotation;
			targetColorData.ValueRW.baseColor = (Vector4)mainCamera.backgroundColor;
		}
	}
}

public partial struct RotateOnDragSys : ISystem {
	EntityQuery query;
	EntityQuery pointerDragQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RotateAroundPivotData>() );
		state.RequireForUpdate( query );
		
		pointerDragQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PointerHeld>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( pointerDragQuery.IsEmpty ) return;

		var inputEntity = GetSingletonEntity<Input>();
		var pointerHeldData = GetComponent<PointerHeld>( inputEntity );

		var delta = pointerHeldData.screenDelta;
		if( delta.x != 0f || delta.y != 0f ) {
			foreach( var (transform, rotationData) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateAroundPivotData>>() ) {
				var euler = new float3( -delta.y, 0f, delta.x ) * rotationData.ValueRO.speed;
				var rot = quaternion.Euler( euler );
				transform.ValueRW = transform.ValueRO.Rotate( rot );
			}
		}
	}
}

public partial struct ChangeBackgroundColor : ISystem {
	public const float speed = 10f;
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<CameraData, TargetColorData>() );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		foreach( var (cameraData, targetColorData) in Query<RefRW<CameraData>, RefRO<TargetColorData>>() ) {
			cameraData.ValueRW.camera.Value.backgroundColor = (Vector4)math.lerp( (Vector4)cameraData.ValueRO.camera.Value.backgroundColor, targetColorData.ValueRO.baseColor, speed * SystemAPI.Time.DeltaTime );
		}
	}
}

public partial struct SyncCameraPos : ISystem {
	EntityQuery query;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<CameraData>().WithNone<RequireInitData>() );
		state.RequireForUpdate( query );
	}
	
	public void OnUpdate( ref SystemState state ) {
		foreach( var (transform, cameraData) in Query<RefRO<LocalToWorld>, RefRW<CameraData>>().WithNone<RequireInitData>() ) {
			cameraData.ValueRW.camera.Value.transform.position = transform.ValueRO.Position;
			cameraData.ValueRW.camera.Value.transform.rotation = transform.ValueRO.Rotation;
			var newOrthoSize = math.max( math.max( transform.ValueRO.Position.x, transform.ValueRO.Position.y ), transform.ValueRO.Position.z ) / 2f;
			cameraData.ValueRW.camera.Value.orthographicSize = newOrthoSize;
			cameraData.ValueRW.orthoSize = newOrthoSize;
		}
	}
}