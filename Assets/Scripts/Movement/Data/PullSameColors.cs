using Unity.Entities;
using UnityEngine;

public class PullSameColors : MonoBehaviour {
	public float speed;
	public float duration;
	public float preferredDist;
	public float dotThreshold;

	public class Baker : Baker<PullSameColors> {

		public override void Bake( PullSameColors auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new PullSameColorsData { speed = auth.speed, duration = auth.duration, preferredDist = auth.preferredDist, dotThreshold = auth.dotThreshold } );
		}
	}
}

public struct PullSameColorsData : IComponentData, IEnableableComponent {
	public float speed;
	public float duration;
	public float elapsedTime;
	public float preferredDist;
	public float dotThreshold;
}