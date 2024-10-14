using Unity.Entities;
using UnityEngine;

public class StarParticle : MonoBehaviour {
	public float speed;

	public class Baker : Baker<StarParticle> {

		public override void Bake( StarParticle auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StarParticleData { speed = auth.speed } );
		}
	}
}

public struct StarParticleData : IComponentData {
	public float speed;
}