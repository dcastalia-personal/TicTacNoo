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

[UpdateBefore(typeof(ClearMatchableColorChangedSys))]
public partial struct EnablePullInOrbitSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PullInOrbitData>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (pullData, pullEnabled, matchableColor, colorChanged, self) 
		        in Query<RefRW<PullInOrbitData>, EnabledRefRW<PullInOrbitData>, RefRO<MatchableColor>, EnabledRefRO<MatchableColorChanged>>()
			        .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState).WithEntityAccess() ) {
			if( matchableColor.ValueRO.value.Equals( GetSingleton<GameColorData>().neutral ) ) continue;
			if( !colorChanged.ValueRO ) continue;
			var cameraEntity = GetSingletonEntity<CameraData>();
			var cameraLtw = GetComponent<LocalToWorld>( cameraEntity );
			pullData.ValueRW.axisOfRotation = -cameraLtw.Forward;
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
		new PullInOrbitJob { physicsWorld = physicsWorldSingleton.PhysicsWorld, matchableColLookup = GetComponentLookup<MatchableColor>() }.ScheduleParallel();
		state.CompleteDependency();
	}

	[BurstCompile] partial struct PullInOrbitJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public PhysicsWorld physicsWorld;
		[ReadOnly] public ComponentLookup<MatchableColor> matchableColLookup;
		[ReadOnly] public float deltaTime;
		
		void Execute( Entity self, in PullInOrbitData pullInOrbitData, in LocalToWorld transform ) {
			
			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			if( !physicsWorld.OverlapSphere( transform.Position, pullInOrbitData.range, ref hits, pullInOrbitData.physicsFilter ) ) return;

			var myBaseColor = matchableColLookup[ self ].value;

			foreach( var hit in hits ) {
				if( hit.Entity == self ) continue;
				if( !matchableColLookup.TryGetComponent( hit.Entity, out MatchableColor hitColor ) ) continue;
				if( !hitColor.value.Equals( myBaseColor ) ) continue;
				
				var dir = hit.SurfaceNormal;
				var normalizedDist = hit.Distance / pullInOrbitData.range;
				var tangent = math.cross( dir, pullInOrbitData.axisOfRotation );
				var falloff = pullInOrbitData.falloff.Value.Sample( normalizedDist );
				var gravity = dir * pullInOrbitData.gravity * (1f - falloff);

				var impulse = tangent * pullInOrbitData.speed + gravity;
				
				physicsWorld.ApplyLinearImpulse( hit.RigidBodyIndex, impulse );
				
				var myBodyIndex = physicsWorld.GetRigidBodyIndex( self );
				physicsWorld.ApplyAngularImpulse( myBodyIndex, pullInOrbitData.axisOfRotation * pullInOrbitData.rotSpeed );
			}
		}
	}
}