using Unity.Entities;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {

	public class Baker : Baker<LookAtCamera> {

		public override void Bake( LookAtCamera auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new LookAtCameraData {} );
		}
	}
}

public struct LookAtCameraData : IComponentData {}