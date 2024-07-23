using Unity.Entities;
using UnityEngine;

public class InGame : MonoBehaviour {

	public class Baker : Baker<InGame> {

		public override void Bake( InGame auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new InGameData {} );
		}
	}
}

public struct InGameData : IComponentData {}