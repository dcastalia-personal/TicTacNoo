using Unity.Entities;
using UnityEngine;

public class InGamePrefabs : MonoBehaviour {
	public GameObject bridgeBeam;

	public class Baker : Baker<InGamePrefabs> {

		public override void Bake( InGamePrefabs auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new InGamePrefabsData { bridgeBeam = GetEntity( auth.bridgeBeam, TransformUsageFlags.Dynamic ) } );
		}
	}
}

public struct InGamePrefabsData : IComponentData {
	public Entity bridgeBeam;
}