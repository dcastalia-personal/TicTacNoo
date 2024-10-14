using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadCommonIfMissing : MonoBehaviour {
	
	#if UNITY_EDITOR

	public GameState gameStatePrefab;
	public static int overrideStartSceneIndex = -1;

	[RuntimeInitializeOnLoadMethod]
	void ResetStatics() {
		overrideStartSceneIndex = -1;
	}

	// will only execute if run from the unbaked scene in Unity
	void Awake() {
		var currentScene = SceneManager.GetActiveScene();

		for( int index = 0; index < gameStatePrefab.levels.Count; index++ ) {
			SceneAsset sceneAsset = gameStatePrefab.levels[ index ].scene;

			if( sceneAsset.name == currentScene.name ) {
				overrideStartSceneIndex = index;
				break;
			}
		}

		SceneManager.LoadScene( 0 );
	}
	// public UnityEditor.SceneAsset common;

	// public class Baker : Baker<LoadCommonIfMissing> {
	//
	// 	public override void Bake( LoadCommonIfMissing auth ) {
	// 		var self = GetEntity( TransformUsageFlags.None );
	// 		AddComponent( self, new LoadCommonIfMissingData { commonData = new EntitySceneReference( auth.common ) } );
	// 	}
	// }
	
	#endif
}

// public struct LoadCommonIfMissingData : IComponentData {
// 	public EntitySceneReference commonData;
// }