using Unity.Entities;
using UnityEngine;

public class BeamTarget : MonoBehaviour {

	public class Baker : Baker<BeamTarget> {

		public override void Bake( BeamTarget auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new BeamTargetData {} );
		}
	}
}

public struct BeamTargetData : IComponentData {
	
}