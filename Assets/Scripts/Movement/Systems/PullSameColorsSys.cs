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
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullSameColorsData, TargetColorData>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;
		
		var levelColors = GetSingletonBuffer<LevelColorData>();
        
		var pullersByColor = new NativeParallelMultiHashMap<float4, SpeedByPosition>( levelColors.Length, Allocator.TempJob );
		var buildPullersJob = new BuildPullersByColorJob { pullersByColor = pullersByColor.AsParallelWriter() }.ScheduleParallel( state.Dependency );
		var pullSameColorsJob = new PullSameColorsJob { pullersByColor = pullersByColor.AsReadOnly() }.ScheduleParallel( buildPullersJob );

		pullSameColorsJob.Complete();

		pullersByColor.Dispose();
	}
	
	[BurstCompile] partial struct BuildPullersByColorJob : IJobEntity {
		public NativeParallelMultiHashMap<float4, SpeedByPosition>.ParallelWriter pullersByColor;
		
		void Execute( Entity self, in PullSameColorsData pullSameColorsData, in TargetColorData colorData, in LocalTransform transform ) {
			if( !colorData.baseColor.Equals( colorData.defaultColor ) ) {
				pullersByColor.Add( colorData.baseColor, new SpeedByPosition { speed = pullSameColorsData.speed, position = transform.Position, puller = self, preferredDistance = pullSameColorsData.preferredDist } );
			}
		}
	}

	[BurstCompile] partial struct PullSameColorsJob : IJobEntity {
		[ReadOnly] public NativeParallelMultiHashMap<float4, SpeedByPosition>.ReadOnly pullersByColor;
		
		void Execute( Entity self, in TargetColorData pullableColorData, ref PhysicsVelocity velocity, in PhysicsMass mass, in LocalTransform transform ) {
			if( !pullersByColor.TryGetFirstValue( pullableColorData.baseColor, out SpeedByPosition speedByPosition, out NativeParallelMultiHashMapIterator<float4> iterator ) ) return;

			do {
				if( speedByPosition.puller == self ) continue;
				var dirToPuller = math.normalize( speedByPosition.position - transform.Position );
				var preferredPosition = speedByPosition.position + -dirToPuller * speedByPosition.preferredDistance;
				var dirToPreferredPosition = math.normalize( preferredPosition - transform.Position );

				velocity.ApplyLinearImpulse( mass, dirToPreferredPosition * speedByPosition.speed );
				
			} while( pullersByColor.TryGetNextValue( out speedByPosition, ref iterator ) );
		}
	}

	struct SpeedByPosition {
		public float speed;
		public float3 position;
		public float preferredDistance;
		public Entity puller;
	}
}