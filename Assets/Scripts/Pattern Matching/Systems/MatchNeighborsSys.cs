using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

public partial struct InitMatchNeighborsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var matchNeighborsData in Query<RefRO<MatchNeighborsData>>() ) {
			var radiusTransform = GetComponent<LocalTransform>( matchNeighborsData.ValueRO.radiusDisplay );
			radiusTransform.Scale = matchNeighborsData.ValueRO.radius * 2f;
			SetComponent( matchNeighborsData.ValueRO.radiusDisplay, radiusTransform );
		}
	}
}

public partial struct MatchNeighborsSys : ISystem {
	EntityQuery query;
	ComponentLookup<TargetColorData> targetColorDataLookup;
	ComponentLookup<MatchingNeighbors> matchingNeighborsLookup;
	ComponentLookup<TargetColorDataChanged> targetColorDataChangedLookup;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, TargetColorData>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<InGameData>();
		
		// pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Pressed>() );
		targetColorDataLookup = state.GetComponentLookup<TargetColorData>( isReadOnly: false );
		matchingNeighborsLookup = state.GetComponentLookup<MatchingNeighbors>( isReadOnly: false );
		targetColorDataChangedLookup = state.GetComponentLookup<TargetColorDataChanged>( isReadOnly: false );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		// if( pressedQuery.IsEmpty ) return;
		targetColorDataLookup.Update( ref state );
		matchingNeighborsLookup.Update( ref state );
		targetColorDataChangedLookup.Update( ref state );

		new MatchNeighborsJob {
			physicsWorld = GetSingleton<PhysicsWorldSingleton>(), 
			targetColorDataLookup = targetColorDataLookup, 
			matchingNeighborsLookup = matchingNeighborsLookup,
			targetColorDataChangedLookup = targetColorDataChangedLookup,
		}.Schedule();
	}

	[WithAll(typeof(TargetColorDataChanged))]
	[BurstCompile] partial struct MatchNeighborsJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		public ComponentLookup<TargetColorData> targetColorDataLookup;
		public ComponentLookup<MatchingNeighbors> matchingNeighborsLookup;
		public ComponentLookup<TargetColorDataChanged> targetColorDataChangedLookup;
		
		void Execute( Entity self, ref MatchNeighborsData matchNeighborsData, in LocalTransform transform ) {
			var myColorData = targetColorDataLookup[ self ];
			if( myColorData.baseColor.Equals( myColorData.defaultColor ) ) return; // if you've just switched to a neutral color

			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			const int matchingMask = 1 << 0;
			var filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask };

			if( !physicsWorld.OverlapSphere( transform.Position, matchNeighborsData.radius, ref hits, filter ) ) return;

			foreach( var hit in hits ) {
				if( hit.Entity == self ) continue;
				targetColorDataLookup[hit.Entity] = myColorData;
				targetColorDataChangedLookup.SetComponentEnabled( hit.Entity, true );
			}
			
			matchingNeighborsLookup.SetComponentEnabled( self, true );
			targetColorDataChangedLookup.SetComponentEnabled( self, false ); // consume this here for this type of entity instead of at the normal time, which is prevented by PreventClearColorChanges
		}
	}
}

[UpdateAfter(typeof(MatchNeighborsSys))] [UpdateBefore(typeof(AbortMatchNeighborsSys))]
public partial struct DisableNeighborMatchingForOneStepSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, TargetColorDataChanged, PreventMatchingNeighborsThisStep>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		state.EntityManager.SetComponentEnabled<PreventMatchingNeighborsThisStep>( query, true );
	}
}

public partial struct ResumeNeighborMatchingAfterStepSys : ISystem {
	EntityQuery query;
	EntityQuery finishedSteppingQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, PreventMatchingNeighborsThisStep>() );
		state.RequireForUpdate( query );
		
		finishedSteppingQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerFinishedStepping>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( finishedSteppingQuery.IsEmpty ) return;

		foreach( var preventMatching in Query<EnabledRefRW<PreventMatchingNeighborsThisStep>>().WithAll<MatchNeighborsData>() ) {
			preventMatching.ValueRW = false;
		}
	}
}

[UpdateAfter(typeof(MatchNeighborsSys))]
public partial struct AbortMatchNeighborsSys : ISystem {
	EntityQuery query;
	ComponentLookup<TargetColorData> targetColorDataLookup;
	EntityQuery stepQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		targetColorDataLookup = state.GetComponentLookup<TargetColorData>( isReadOnly: false );
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, TargetColorData, LocalTransform, MatchingNeighbors>().WithNone<PreventMatchingNeighborsThisStep>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;
		
		targetColorDataLookup.Update( ref state );
		new AbortMatchNeighborsJob { physicsWorld = GetSingleton<PhysicsWorldSingleton>(), targetColorDataLookup = targetColorDataLookup }.Schedule();
	}

	[WithAll(typeof(MatchingNeighbors))] [WithNone(typeof(TargetColorDataChanged))] [WithNone(typeof(PreventMatchingNeighborsThisStep))]
	[BurstCompile] partial struct AbortMatchNeighborsJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		public ComponentLookup<TargetColorData> targetColorDataLookup;
		
		void Execute( Entity self, in MatchNeighborsData matchNeighborsData, EnabledRefRW<MatchingNeighbors> matchNeighborsEnabled, in LocalTransform transform ) {
			var myColorData = targetColorDataLookup[ self ];

			if( myColorData.baseColor.Equals( myColorData.defaultColor ) ) return;
			
			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			const int matchingMask = 1 << 0;
			var filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask };

			if( !physicsWorld.OverlapSphere( transform.Position, matchNeighborsData.radius, ref hits, filter ) ) return;

			foreach( var hit in hits ) {
				if( hit.Entity == self ) continue;
				if( !targetColorDataLookup.HasComponent( hit.Entity ) ) continue;
				var otherColor = targetColorDataLookup[ hit.Entity ];
				if( otherColor.baseColor.Equals( otherColor.defaultColor ) ) continue;
				if( otherColor.baseColor.Equals( myColorData.baseColor ) ) continue;

				myColorData.baseColor = myColorData.defaultColor;
				myColorData.emission = 0f;
				targetColorDataLookup[ self ] = myColorData;
				matchNeighborsEnabled.ValueRW = false;
			}
		}
	}
}