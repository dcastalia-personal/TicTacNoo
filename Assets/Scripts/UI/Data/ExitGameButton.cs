using Unity.Entities;
using UnityEngine;

public class ExitGameButton : MonoBehaviour {

	public class Baker : Baker<ExitGameButton> {

		public override void Bake( ExitGameButton auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ExitGameButtonData {} );
		}
	}
}

public struct ExitGameButtonData : IComponentData {
	
}