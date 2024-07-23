using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

public partial struct TweenScaleSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ScaleOnCurveData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		
		foreach( var (scaleOnCurveData, scaleOnCurveDataEnabled, transform) in Query<RefRW<ScaleOnCurveData>, EnabledRefRW<ScaleOnCurveData>, RefRW<LocalTransform>>() ) {
			scaleOnCurveData.ValueRW.elapsedTime += deltaTime * scaleOnCurveData.ValueRO.speed;
			// Debug.Log( $"Incrementing elapsed time to {scaleOnCurveData.ValueRW.elapsedTime}" );
			
			if( scaleOnCurveData.ValueRO.elapsedTime >= 1f ) {
				switch( scaleOnCurveData.ValueRO.finishMode ) {
					case FinishMode.Clamp: scaleOnCurveData.ValueRW.elapsedTime = 1f; break;
					case FinishMode.Reset: scaleOnCurveData.ValueRW.elapsedTime = 0f;
						// Debug.Log( $"Resetting elapsed time to {scaleOnCurveData.ValueRW.elapsedTime}" );
						break;
				}
				
				scaleOnCurveDataEnabled.ValueRW = false;
			}

			transform.ValueRW.Scale = scaleOnCurveData.ValueRO.curve.Value.Sample( scaleOnCurveData.ValueRO.elapsedTime );
		}
	}
}