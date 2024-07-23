using Unity.Entities;
using UnityEngine;

public class MenuAnimation : MonoBehaviour {
	public GameObject flare;
	public GameObject outTransitionPrefab;
	public float statsInDelay;

	public class Baker : Baker<MenuAnimation> {

		public override void Bake( MenuAnimation auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MenuAnimationData {
				flare = GetEntity( auth.flare, TransformUsageFlags.Dynamic ), 
				outTransitionPrefab = GetEntity( auth.outTransitionPrefab, TransformUsageFlags.None ),
				statsInDelay = auth.statsInDelay
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
}

public struct ExitMenuData : IComponentData {}