using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

public partial struct EnablePullInOrbitSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullInOrbitData>() );
		state.RequireForUpdate( query );
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepped>() );

		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;

		foreach( var (pullEnabled, targetColor) in Query<EnabledRefRW<PullInOrbitData>, RefRO<TargetColorData>>().WithAll<PullInOrbitData>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState) ) {
			if( targetColor.ValueRO.baseColor.Equals( targetColor.ValueRO.defaultColor ) ) continue;
			pullEnabled.ValueRW = true;
		}
	}
}

[UpdateInGroup(typeof(PhysicsSimulationGroup))]
public partial struct PullInOrbitSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullInOrbitData>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;
		
		var physicsWorldSingleton = GetSingleton<PhysicsWorldSingleton>();
		new PullInOrbitJob { physicsWorld = physicsWorldSingleton.PhysicsWorld }.ScheduleParallel();
		state.CompleteDependency();
	}

	[BurstCompile] partial struct PullInOrbitJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public PhysicsWorld physicsWorld;
		
		void Execute( Entity self, in PullInOrbitData pullInOrbitData, in LocalTransform transform ) {
			
			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			if( !physicsWorld.OverlapSphere( transform.Position, pullInOrbitData.range, ref hits, pullInOrbitData.physicsFilter ) ) return;

			foreach( var hit in hits ) {
				if( hit.Entity == self ) continue;
				var dir = hit.SurfaceNormal;
				var normalizedDist = hit.Distance / pullInOrbitData.range;
				var tangent = math.cross( dir, transform.Up() );
				var falloff = pullInOrbitData.falloff.Value.Sample( normalizedDist );
				var gravity = dir * pullInOrbitData.gravity * (1f - falloff);

				var impulse = tangent * pullInOrbitData.speed * falloff + gravity;

				physicsWorld.ApplyLinearImpulse( hit.RigidBodyIndex, impulse );
			}
		}
	}
}