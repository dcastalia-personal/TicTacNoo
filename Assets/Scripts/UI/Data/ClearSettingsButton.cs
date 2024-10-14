using Unity.Entities;
using UnityEngine;

public class ClearSettingsButton : MonoBehaviour {

	public class Baker : Baker<ClearSettingsButton> {

		public override void Bake( ClearSettingsButton auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ClearSettingsButtonData {} );
		}
	}
}

public struct ClearSettingsButtonData : IComponentData {
	
}