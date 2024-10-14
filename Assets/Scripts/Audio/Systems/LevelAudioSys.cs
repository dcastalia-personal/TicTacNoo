using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitLevelAudioSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelAudioData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (levelAudioData, self) in Query<RefRO<LevelAudioData>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			var musicInstance = ecb.Instantiate( levelAudioData.ValueRO.music );
			var levelAudioDataCopy = levelAudioData.ValueRO;
			levelAudioDataCopy.musicInstance = musicInstance;

			ecb.SetComponent( self, levelAudioDataCopy );
		}
	}
}