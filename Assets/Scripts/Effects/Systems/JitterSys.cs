using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PrePresentationSystemGroup))]
public partial struct PreJitterSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<JitterData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		new PreJitterJob { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel();
	}

	[BurstCompile] partial struct PreJitterJob : IJobEntity {
		public float deltaTime;
		
		void Execute( ref JitterData jitterData, ref LocalToWorld transform, EnabledRefRW<JitterData> jitterEnabled ) {
			
			var displacement = new float3 { 
				x = noise.cnoise( new float2( jitterData.time, jitterData.noiseEntryPointsPerAxis.x ) ),
				y = noise.cnoise( new float2( jitterData.time, jitterData.noiseEntryPointsPerAxis.y ) ),
				z = noise.cnoise( new float2( jitterData.time, jitterData.noiseEntryPointsPerAxis.z ) ),
			};

			jitterData.time += deltaTime * jitterData.speed;
			jitterData.intensity = math.clamp( jitterData.intensity + deltaTime * jitterData.acceleration, 0f, 1f );

			var translation = displacement * jitterData.power * jitterData.easing.Value.Sample( jitterData.intensity ) * deltaTime;
			var trs = transform.Value;
			var pos = trs.c3;
			pos += new float4( translation, 0f );
			trs.c3 = pos;
			
			transform.Value = trs;

			if( jitterData.intensity == 0f ) {
				jitterEnabled.ValueRW = false;
				jitterData.acceleration = math.abs( jitterData.acceleration );
			}
		}
	}
}