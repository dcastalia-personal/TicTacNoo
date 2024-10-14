using Unity.Entities;
using UnityEngine;

public class LevelAudio : MonoBehaviour {
	public GameObject music;

	public class Baker : Baker<LevelAudio> {

		public override void Bake( LevelAudio auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LevelAudioData {
				music = GetEntity( auth.music, TransformUsageFlags.Dynamic ),
			} );
		}
	}
}

public struct LevelAudioData : IComponentData {
	public Entity music;
	public Entity musicInstance;
}