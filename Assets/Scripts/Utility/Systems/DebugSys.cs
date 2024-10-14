using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct LogOnInitSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LogOnInitData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (logOnInitData, self) in Query<RefRO<LogOnInitData>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			if( TryGetSingletonBuffer<Level>( out var levels ) ) {
				Debug.Log( $"Initializing {levels.Length} levels" );
			}
			else {
				Debug.Log( $"No levels buffer found" );
			}
		}
	}
}