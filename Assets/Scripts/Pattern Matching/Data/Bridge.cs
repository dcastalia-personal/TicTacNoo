using Unity.Entities;
using UnityEngine;

public class Bridge : MonoBehaviour {

	public class Baker : Baker<Bridge> {

		public override void Bake( Bridge auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new BridgeData {} );
		}
	}
}

public struct BridgeData : IComponentData {
	public Entity beam;
}