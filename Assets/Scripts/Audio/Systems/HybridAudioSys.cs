using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitHybridAudioSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<HybridAudioData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (hybridAudioPool, self) in Query<HybridAudioPool>().WithAll<HybridAudioData, RequireInitData>().WithEntityAccess() ) {
			var musicVolume = PlayerPrefs.GetFloat( "Music", 1f );
			var effectsVolume = PlayerPrefs.GetFloat( "Effects", 1f );
			hybridAudioPool.groups[ 0 ].audioMixer.SetFloat( "Music", math.log10( musicVolume ) * 20f );
			hybridAudioPool.groups[ 0 ].audioMixer.SetFloat( "Effects", math.log10( effectsVolume ) * 20f );
		}
	}
}

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitAudioDefSys : ISystem {
	EntityQuery query;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<HybridAudioData>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AudioDefinitionData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var hybridAudioEntity = GetSingletonEntity<HybridAudioData>();
		var hybridAudio = GetComponent<HybridAudioData>( hybridAudioEntity );
		var randomnessData = GetComponent<RandomnessData>( hybridAudioEntity );
		var hybridAudioPool = state.EntityManager.GetComponentObject<HybridAudioPool>( hybridAudioEntity );
		var pool = hybridAudioPool.pool;

		pool ??= new();
		
		foreach( var (definition, audioRefs, transform, self) 
		        in Query<RefRW<AudioDefinitionData>, DynamicBuffer<AudioRef>, RefRO<LocalTransform>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			var sourceToUse = pool.FirstOrDefault( source => !source.isPlaying );

			if( !sourceToUse ) {
				sourceToUse = Object.Instantiate( hybridAudio.template.Value ).GetComponent<AudioSource>();
				pool.Add( sourceToUse );
			}

			var skipPrevClip = definition.ValueRO.lastClipIndexPlayed == -1 && audioRefs.Length > 1;
			var numCandidates = skipPrevClip ? audioRefs.Length - 1 : audioRefs.Length;
			var clipCandidates = new NativeList<AudioRef>( numCandidates, Allocator.Temp );

			for( int index = 0; index < audioRefs.Length; index++ ) {
				if( skipPrevClip && index == definition.ValueRO.lastClipIndexPlayed ) continue;
				clipCandidates.AddNoResize( audioRefs[ index ] );
			}

			var clipIndexToUse = randomnessData.rng.NextInt( 0, clipCandidates.Length );
			definition.ValueRW.lastClipIndexPlayed = clipIndexToUse;
			
			sourceToUse.clip = clipCandidates[ clipIndexToUse ].clip;
			sourceToUse.loop = definition.ValueRO.loop;
			sourceToUse.volume = 0f;
			sourceToUse.spatialBlend = definition.ValueRO.mix;
			definition.ValueRW.duration = sourceToUse.clip.length;
			sourceToUse.transform.position = transform.ValueRO.Position;
			sourceToUse.outputAudioMixerGroup = hybridAudioPool.groups[ definition.ValueRO.group ];
			sourceToUse.Play();

			definition.ValueRW.source.Value = sourceToUse;

			SetComponent( hybridAudioEntity, randomnessData );
		}
	}
}

public partial struct FadeInAudioSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AudioDefinitionData, FadeIn>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<HybridAudioData>();
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (definition, fade, fadeEnabled, self ) in Query<RefRO<AudioDefinitionData>, RefRO<FadeIn>, EnabledRefRW<FadeIn>>().WithEntityAccess() ) {
			var normTime = 0f;
			
			if( fade.ValueRO.duration == 0f ) {
				normTime = 1f;
			}
			else {
				normTime = definition.ValueRO.time / fade.ValueRO.duration;
			}

			bool completed = false;
			if( normTime >= 1f ) {
				normTime = 1f;
				completed = true;
			}

			definition.ValueRO.source.Value.volume = fade.ValueRO.fadeCurve.Value.Sample( normTime ) * definition.ValueRO.volume;

			if( completed ) fadeEnabled.ValueRW = false;
		}
	}
}

public partial struct UpdateAudioDefinitionsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AudioDefinitionData>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<HybridAudioData>();
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (definition, transform, self) in Query<RefRW<AudioDefinitionData>, RefRO<LocalTransform>>().WithEntityAccess() ) {
			definition.ValueRW.time += SystemAPI.Time.DeltaTime;
			definition.ValueRO.source.Value.transform.position = transform.ValueRO.Position;
			if( definition.ValueRO.loop ) continue;
			
			if( definition.ValueRO.time >= definition.ValueRO.duration ) {
				definition.ValueRO.source.Value.Stop();
				var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
				var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

				ecb.DestroyEntity( self );
			}
		}
	}
}

public partial struct FadeOutAudioSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AudioDefinitionData, FadeOut>() );
		state.RequireForUpdate( query );
		
		state.RequireForUpdate<HybridAudioData>();
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (definition, fade, self ) in Query<RefRO<AudioDefinitionData>, RefRW<FadeOut>>().WithEntityAccess() ) {
			if( fade.ValueRO.time == 0f ) {
				fade.ValueRW.startVolume = definition.ValueRO.source.Value.volume;
			}

			var normTime = fade.ValueRO.time / fade.ValueRO.duration;

			bool completed = false;
			if( normTime >= 1f ) {
				normTime = 1f;
				completed = true;
			}

			definition.ValueRO.source.Value.volume = fade.ValueRO.fadeCurve.Value.Sample( normTime ) * fade.ValueRO.startVolume * definition.ValueRO.volume;

			if( completed ) {
				fade.ValueRW.time = 0f;
				definition.ValueRO.source.Value.Stop();
				var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
				var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
				ecb.DestroyEntity( self );
				
				continue;
			}
			
			fade.ValueRW.time += SystemAPI.Time.DeltaTime;
		}
	}
}