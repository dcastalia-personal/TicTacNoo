using Unity.Entities;
using UnityEngine;

public class MenuAnimation : MonoBehaviour {
	public GameObject flare;
	public GameObject outTransitionPrefab;
	public float statsInDelay;
	public GameObject music;
	public GameObject outAudio;
	public GameObject flareAudio;

	public class Baker : Baker<MenuAnimation> {

		public override void Bake( MenuAnimation auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MenuAnimationData {
				flare = GetEntity( auth.flare, TransformUsageFlags.Dynamic ), 
				outTransitionPrefab = GetEntity( auth.outTransitionPrefab, TransformUsageFlags.None ),
				musicPrefab = GetEntity( auth.music, TransformUsageFlags.Dynamic ),
				statsInDelay = auth.statsInDelay,
				outAudio = GetEntity( auth.outAudio, TransformUsageFlags.Dynamic ),
				flareAudio = GetEntity( auth.flareAudio, TransformUsageFlags.Dynamic ),
			} );
		}
	}
}

public struct MenuAnimationData : IComponentData {
	public Entity flare;
	public Entity outTransitionPrefab;
	public float statsInDelay;
	public float elapsedTime;
	public bool displayedStats;
	
	public Entity musicPrefab;
	public Entity outAudio;
	public Entity flareAudio;

	public Entity musicInstance;
}

public struct ExitMenuData : IComponentData {}