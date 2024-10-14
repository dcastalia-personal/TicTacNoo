using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public class GameState : MonoBehaviour {
	public GameObject inGamePrefab;
	public GameObject successAnimationPrefab;
	public GameObject failureAnimationPrefab;
	public GameObject failureAckPrefab;
	public GameObject successAckPrefab;
	
	public int startLevel;
	
	#if UNITY_EDITOR

	public List<SceneDescription> levels = new();
	
	public class Baker : Baker<GameState> {

		public override void Bake( GameState auth ) {
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new GameStateData {
				startLevel = auth.startLevel,
				successAnimationPrefab = GetEntity( auth.successAnimationPrefab, TransformUsageFlags.None ), 
				failureAnimationPrefab = GetEntity( auth.failureAnimationPrefab, TransformUsageFlags.None ),
				failureAckPrefab = GetEntity( auth.failureAckPrefab, TransformUsageFlags.None ),
				successAckPrefab = GetEntity( auth.successAckPrefab, TransformUsageFlags.None ),
				inGamePrefab = GetEntity( auth.inGamePrefab, TransformUsageFlags.None ),
			} );
			
			var levels = AddBuffer<Level>( self );
			for( int index = 0; index < auth.levels.Count; index++ ) {
				DependsOn( auth.levels[ index ].scene );
				levels.Add( new Level { reference = new EntitySceneReference( auth.levels[ index ].scene ), bestTime = auth.levels[index].bestTime, name = auth.levels[index].name } );
			}

			AddComponent( self, new VictoryEnabled() ); SetComponentEnabled<VictoryEnabled>( self, false );
		}
	}
	
	#endif
}

public struct GameStateData : IComponentData {
	public int curLevelIndex;
	public int nextLevel;
	public int startLevel;
	
	public Entity inGamePrefab;
	public Entity successAnimationPrefab;
	public Entity failureAnimationPrefab;
	public Entity successAckPrefab;
	public Entity failureAckPrefab;

	public Entity curLoadedScene;
	
	public const int maxNumStars = 10;
}

public struct SwitchLevel : IComponentData {}

[InternalBufferCapacity( 0 )]
public struct Level : IBufferElementData {
	public EntitySceneReference reference;
	public float bestTime;
	public FixedString32Bytes name;
}

public struct VictoryEnabled : IComponentData, IEnableableComponent {}

#if UNITY_EDITOR
[Serializable]
public class SceneDescription {
	public UnityEditor.SceneAsset scene;
	public float bestTime;
	public string name;
}
#endif