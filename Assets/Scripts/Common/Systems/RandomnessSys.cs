using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using Random = Unity.Mathematics.Random;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitRandomnessSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RandomnessData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var rngRef = new NativeReference<Random>( new Random( (uint)System.DateTime.Now.Second ), Allocator.TempJob );
		var randomnessJob = new RandomnessJob { rngRef = rngRef }.Schedule( query, state.Dependency );
		randomnessJob.Complete();
		
		rngRef.Dispose();
	}

	[BurstCompile] partial struct RandomnessJob : IJobEntity {
		public NativeReference<Random> rngRef;
		
		void Execute( ref RandomnessData randomness ) {
			var rng = rngRef.Value;
			randomness.rng = new Random( rng.NextUInt() );
			rngRef.Value = rng;
		}
	}
}

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(InitRandomnessSys) )]
public partial struct InitRandomAnimModifierSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RandomAnimModifierData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var gameStateEntity = GetSingletonEntity<GameStateData>();
		var randomnessRef = new NativeReference<RandomnessData>( GetComponent<RandomnessData>( gameStateEntity ), Allocator.TempJob );
		var initAnimModifierJob = new InitRandomAnimModifierJob { randomnessRef = randomnessRef }.Schedule( query, state.Dependency );
		initAnimModifierJob.Complete();

		SetComponent( gameStateEntity, randomnessRef.Value );
		randomnessRef.Dispose();
	}

	[BurstCompile] partial struct InitRandomAnimModifierJob : IJobEntity {
		public NativeReference<RandomnessData> randomnessRef;
		
		void Execute( ref RandomAnimModifierData randomAnimModifier ) {
			var randomness = randomnessRef.Value;
			var rng = randomness.rng;
			randomAnimModifier.value = rng.NextFloat( randomAnimModifier.min, randomAnimModifier.max );
			// randomAnimModifier.value = rng.NextBool() ? randomAnimModifier.value : -randomAnimModifier.value;
			randomAnimModifier.randomDir = rng.NextFloat3( new float3( -1f, -1f, -1f ), new float3( 1f, 1f, 1f ) );

			randomness.rng = rng;
			randomnessRef.Value = randomness;
		}
	}
}