using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitRandomDirSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RandomDirectionData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var rng = new Random( seed: (uint)(SystemAPI.Time.ElapsedTime * 100f) );
		foreach( var randomDirectionData in Query<RefRW<RandomDirectionData>>() ) {
			var randomDir = rng.NextFloat3( new float3( -1f, -1f, -1f ), new float3( 1f, 1f, 1f ) );
			randomDir = math.normalize( new float3(
				randomDir.x * randomDirectionData.ValueRO.modifiers.x,
				randomDir.y * randomDirectionData.ValueRO.modifiers.y,
				randomDir.z * randomDirectionData.ValueRO.modifiers.z
			) );

			var randomLength = rng.NextFloat( randomDirectionData.ValueRO.lengthMin, randomDirectionData.ValueRO.lengthMax );
			randomDirectionData.ValueRW.value = randomDir * randomLength;
		}
	}
}