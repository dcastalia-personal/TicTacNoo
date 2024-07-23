using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Entities.SystemAPI;

#if UNITY_EDITOR
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct LoadCommonIfMissingSys : ISystem {

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		EntityQuery query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadCommonIfMissingData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( TryGetSingleton( out GameStateData _ ) ) return; // started the game not from the "Start" scene where the game state lives
		SceneManager.LoadScene( 0, LoadSceneMode.Additive );
	}
}
#endif