using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using Collider = Unity.Physics.Collider;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
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
	ComponentLookup<MatchingNeighbors> matchingNeighborsLookup;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, TargetColorData>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<InGameData>();
		
		matchingNeighborsLookup = state.GetComponentLookup<MatchingNeighbors>( isReadOnly: false );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		matchingNeighborsLookup.Update( ref state );

		var cameraEntity = GetSingletonEntity<CameraData>();

		new MatchNeighborsJob {
			physicsWorld = GetSingleton<PhysicsWorldSingleton>(), 
			matchableColLookup = GetComponentLookup<MatchableColor>(), 
			matchingNeighborsLookup = matchingNeighborsLookup,
			matchableColChangedLookup = GetComponentLookup<MatchableColorChanged>(),
			neutralColor = GetSingleton<GameColorData>().neutral,
			cameraData = GetComponent<CameraData>( cameraEntity ),
			cameraLtw = GetComponent<LocalToWorld>( cameraEntity ),
		}.Schedule();
	}

	[WithAll(typeof(MatchableColorChanged))]
	[BurstCompile] partial struct MatchNeighborsJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		public ComponentLookup<MatchableColor> matchableColLookup;
		public ComponentLookup<MatchingNeighbors> matchingNeighborsLookup;
		public ComponentLookup<MatchableColorChanged> matchableColChangedLookup;
		public float4 neutralColor;
		public CameraData cameraData;
		public LocalToWorld cameraLtw;

		void Execute( Entity self, ref MatchNeighborsData matchNeighborsData, ref TargetColorData targetColorData, in LocalTransform transform ) {
			var myColorData = matchableColLookup[ self ];
			if( myColorData.value.Equals( neutralColor ) ) return; // if you've just switched to a neutral color

			NativeArray<Entity> methodAgnosticHits;

			const int matchingMask = 1 << 0;
			var filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask };

			if( cameraData.orthographic ) {
				var hits = new NativeList<DistanceHit>( Allocator.Temp );
				physicsWorld.OverlapSphere( transform.Position, matchNeighborsData.radius, ref hits, filter );

				methodAgnosticHits = new NativeArray<Entity>( hits.Length, Allocator.Temp );
				for( int index = 0; index < hits.Length; index++ ) methodAgnosticHits[ index ] = hits[ index ].Entity;
			}
			else {
				// construct convex collider out of points
				const int coneSides = 32;
				var radsPerSide = math.TAU / coneSides;
				var vertices = new NativeArray<float3>( coneSides + 1, Allocator.Temp );
				vertices[ 0 ] = float3.zero;

				const float dilation = 0.16f; // I'm not sure why this is necessary; maybe it has something to do with the bevel radius of the level colliders
				var localZ = math.distance( cameraLtw.Position, transform.Position );
				var stepsToExtent = matchNeighborsData.range / localZ;

				var dirFromCamera = math.normalize( transform.Position - cameraLtw.Position );
				var localToWorldRot = quaternion.LookRotation( dirFromCamera, cameraLtw.Up );

				for( int index = 0; index < coneSides; index++ ) {
					var rotation = quaternion.AxisAngle( new float3( 0f, 0f, 1f ), radsPerSide * index );
					var localPos = math.mul( rotation, new float3( 0f, matchNeighborsData.radius + dilation, localZ ) * stepsToExtent );
					vertices[ index + 1 ] = localPos;
				}

				BlobAssetReference<Collider> collider = ConvexCollider.Create( vertices, new ConvexHullGenerationParameters {}, filter );

				var hits = new NativeList<ColliderCastHit>( Allocator.Temp );
				unsafe {
					physicsWorld.CastCollider( new ColliderCastInput {
						Collider = collider.AsPtr(),
						Start = cameraLtw.Position,
						End = cameraLtw.Position,
						Orientation = localToWorldRot,
					}, ref hits );
				}

				collider.Dispose();

				var distToMatcher = math.distancesq( transform.Position, cameraLtw.Position );

				for( int index = hits.Length - 1; index > -1; index-- ) {
					var distToThisHit = math.distancesq( hits[ index ].Position, cameraLtw.Position );
					if( distToThisHit < distToMatcher ) hits.RemoveAt( index );
				}

				methodAgnosticHits = new NativeArray<Entity>( hits.Length, Allocator.Temp );
				for( int index = 0; index < hits.Length; index++ ) methodAgnosticHits[ index ] = hits[ index ].Entity;
			}

			foreach( var hit in methodAgnosticHits ) {
				if( hit == self ) continue;
				matchableColLookup[hit] = myColorData;
				matchableColChangedLookup.SetComponentEnabled( hit, true );
			}
			
			matchingNeighborsLookup.SetComponentEnabled( self, true );
			targetColorData.baseColor = myColorData.value;
			targetColorData.emission = myColorData.emission;

			matchableColChangedLookup.SetComponentEnabled( self, false ); // consume this here for this type of entity instead of at the normal time, which is prevented by PreventClearColorChanges
		}
	}
}

[UpdateAfter(typeof(MatchNeighborsSys))] [UpdateBefore(typeof(AbortMatchNeighborsSys))]
public partial struct DisableNeighborMatchingForOneStepSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, MatchableColorChanged, PreventMatchingNeighborsThisStep>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (_, self) in Query<RefRO<MatchNeighborsData>>().WithAll<MatchableColorChanged>().WithEntityAccess() ) {
			SetComponentEnabled<PreventMatchingNeighborsThisStep>( self, true );
		}
	}
}

[UpdateAfter(typeof(UpdatePlayerStepSys))]
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
	EntityQuery stepQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchNeighborsData, TargetColorData, LocalTransform, MatchingNeighbors>().WithNone<PreventMatchingNeighborsThisStep>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<InGameData>();
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( stepQuery.IsEmpty ) return;
		
		new AbortMatchNeighborsJob {
			physicsWorld = GetSingleton<PhysicsWorldSingleton>(), 
			matchableColLookup = GetComponentLookup<MatchableColor>(), 
			neutralColor = GetSingleton<GameColorData>().neutral,
			matchableChangedColLookup = GetComponentLookup<MatchableColorChanged>(),
		}.Schedule();
	}

	[WithAll(typeof(MatchingNeighbors))] [WithNone(typeof(MatchableColorChanged))] [WithNone(typeof(PreventMatchingNeighborsThisStep))]
	[BurstCompile] partial struct AbortMatchNeighborsJob : IJobEntity {
		[ReadOnly] public PhysicsWorldSingleton physicsWorld;
		public ComponentLookup<MatchableColor> matchableColLookup;
		public ComponentLookup<MatchableColorChanged> matchableChangedColLookup;
		public float4 neutralColor;
		
		void Execute( Entity self, in MatchNeighborsData matchNeighborsData, EnabledRefRW<MatchingNeighbors> matchNeighborsEnabled, in LocalTransform transform ) {
			var myColorData = matchableColLookup[ self ];

			if( myColorData.value.Equals( neutralColor ) ) return;
			
			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			const int matchingMask = 1 << 0;
			var filter = new CollisionFilter { BelongsTo = matchingMask, CollidesWith = matchingMask };

			if( !physicsWorld.OverlapSphere( transform.Position, matchNeighborsData.radius, ref hits, filter ) ) return;

			foreach( var hit in hits ) {
				if( hit.Entity == self ) continue;
				if( !matchableColLookup.HasComponent( hit.Entity ) ) continue;
				var otherColor = matchableColLookup[ hit.Entity ];
				if( otherColor.value.Equals( neutralColor ) ) continue;
				if( otherColor.value.Equals( myColorData.value ) ) continue;

				myColorData.value = neutralColor;
				myColorData.emission = 0f;
				matchableColLookup[ self ] = myColorData;
				matchableChangedColLookup.SetComponentEnabled( self, true );
				matchNeighborsEnabled.ValueRW = false;
			}
		}
	}
}