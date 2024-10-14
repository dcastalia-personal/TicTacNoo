using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))] 
public partial struct ShareColorSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ShareColorOnContactData, MatchableColor>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var matchableColorLookup = GetComponentLookup<MatchableColor>();
		var matchableColorChangedLookup = GetComponentLookup<MatchableColorChanged>();
		var collisions = GetSingleton<SimulationSingleton>().AsSimulation().CollisionEvents;
		new MatchOtherColorJob {
			collisions = collisions, 
			matchableColorLookup = matchableColorLookup, 
			matchableColorChangedLookup = matchableColorChangedLookup,
			neutralColor = GetSingleton<GameColorData>().neutral,
		}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct MatchOtherColorJob : IJobEntity {
		[ReadOnly] public CollisionEvents collisions;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<MatchableColor> matchableColorLookup;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<MatchableColorChanged> matchableColorChangedLookup;
		public float4 neutralColor;
		
		void Execute( Entity self, in MatchableColor myColor ) {
			if( myColor.value.Equals( neutralColor ) ) return;
			
			foreach( var collision in collisions ) {
				Entity otherEntity;
				if( collision.EntityA != self && collision.EntityB != self ) continue;
				otherEntity = collision.EntityA == self ? collision.EntityB : collision.EntityA;
				
				if( !matchableColorLookup.TryGetComponent( otherEntity, out MatchableColor otherColor ) ) continue;
				var myColorWithTheirEmission = myColor;
				myColorWithTheirEmission.emission = math.max( otherColor.emission, myColor.emission ); // if you're excited, then leave it that way, but if you're neutral, then upgrade
				matchableColorLookup[ otherEntity ] = myColorWithTheirEmission;
				matchableColorChangedLookup.SetComponentEnabled( otherEntity, true );
			}
		}
	}
}