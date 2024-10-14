using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Entities.SystemAPI;

// #if UNITY_EDITOR
// [UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
// public partial struct LoadCommonIfMissingSys : ISystem {
//
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		EntityQuery query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadCommonIfMissingData, RequireInitData>() );
// 		state.RequireForUpdate( query );
// 	}
//
// 	public void OnUpdate( ref SystemState state ) {
// 		Debug.Log( $"Trying to load common" );
// 		if( TryGetSingleton( out GameStateData _ ) ) return; // started the game not from the "Main" scene where the game state lives
// 		var commonData = GetSingleton<LoadCommonIfMissingData>();
// 		SceneSystem.LoadSceneAsync( state.WorldUnmanaged, commonData.commonData, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
//
// 		Debug.Log( $"Loaded common" );
// 	}
// }
// #endif