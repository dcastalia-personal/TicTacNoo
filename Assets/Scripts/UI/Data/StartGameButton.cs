using Unity.Entities;
using UnityEngine;

public class StartGameButton : MonoBehaviour {

	public class Baker : Baker<StartGameButton> {

		public override void Bake( StartGameButton auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StartGameButtonData {} );
		}
	}
}

public struct StartGameButtonData : IComponentData {
	
}