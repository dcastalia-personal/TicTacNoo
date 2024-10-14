using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateBefore(typeof(ClearPlayerStepSys))]
public partial struct EnablePullSameColorsSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullSameColorsData>() );
		state.RequireForUpdate( query );
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepped>() );

		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;

		foreach( var pullSameColorsEnabled in Query<EnabledRefRW<PullSameColorsData>>().WithAll<PullSameColorsData>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			pullSameColorsEnabled.ValueRW = true;
		}
	}
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct PullSameColorsSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullSameColorsData, MatchableColor>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;
		
		var pullersByColor = new NativeParallelMultiHashMap<float4, PullerData>( query.CalculateEntityCount(), Allocator.TempJob );
		var buildPullersJob = new BuildPullersByColorJob { pullersByColor = pullersByColor.AsParallelWriter(), neutralColor = GetSingleton<GameColorData>().neutral }.ScheduleParallel( state.Dependency );
		var pullSameColorsJob = new PullSameColorsJob { pullersByColor = pullersByColor.AsReadOnly() }.ScheduleParallel( buildPullersJob );

		pullSameColorsJob.Complete();

		pullersByColor.Dispose();
	}
	
	[BurstCompile] partial struct BuildPullersByColorJob : IJobEntity {
		public NativeParallelMultiHashMap<float4, PullerData>.ParallelWriter pullersByColor;
		public float4 neutralColor;
		
		void Execute( Entity self, in PullSameColorsData pullSameColorsData, in MatchableColor colorData, in LocalToWorld transform ) {
			if( !colorData.value.Equals( neutralColor ) ) {
				pullersByColor.Add( colorData.value, new PullerData {
					speed = pullSameColorsData.speed, 
					position = transform.Position, 
					puller = self, 
					preferredDistance = pullSameColorsData.preferredDist,
					forward = transform.Forward,
					dotThreshold = pullSameColorsData.dotThreshold,
				} );
			}
		}
	}

	[BurstCompile] partial struct PullSameColorsJob : IJobEntity {
		[ReadOnly] public NativeParallelMultiHashMap<float4, PullerData>.ReadOnly pullersByColor;
		
		void Execute( Entity self, in MatchableColor pullableColorData, ref PhysicsVelocity velocity, in PhysicsMass mass, in LocalToWorld ltw ) {
			if( !pullersByColor.TryGetFirstValue( pullableColorData.value, out PullerData pullerData, out NativeParallelMultiHashMapIterator<float4> iterator ) ) return;

			do {
				if( pullerData.puller == self ) continue;
				var dirToPuller = math.normalize( pullerData.position - ltw.Position );
				var dot = math.max( math.dot( pullerData.forward, -dirToPuller ), pullerData.dotThreshold );
				var remappedDot = math.remap( pullerData.dotThreshold, 1f, 0f, 1f, dot );
				if( dot == 0f ) continue;
				
				var preferredPosition = pullerData.position + -dirToPuller * pullerData.preferredDistance;
				var dirToPreferredPosition = math.normalize( preferredPosition - ltw.Position );

				velocity.ApplyLinearImpulse( mass, dirToPreferredPosition * pullerData.speed * remappedDot );
				
			} while( pullersByColor.TryGetNextValue( out pullerData, ref iterator ) );
		}
	}

	struct PullerData {
		public float speed;
		public float3 position;
		public float preferredDistance;
		public float3 forward;
		public Entity puller;
		public float dotThreshold;
	}
}