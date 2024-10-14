using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class LaserImpact : MonoBehaviour {

	public class Baker : Baker<LaserImpact> {

		public override void Bake( LaserImpact auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new LaserImpactData {} );
			AddComponent( self, new CurrentColorData {} );
		}
	}
}

public struct LaserImpactData : IComponentData {
	
}