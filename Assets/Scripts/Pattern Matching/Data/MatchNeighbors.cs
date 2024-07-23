using Unity.Entities;
using UnityEngine;

public class MatchNeighbors : MonoBehaviour {
	public GameObject display;
	public float radius;
	
	public class Baker : Baker<MatchNeighbors> {

		public override void Bake( MatchNeighbors auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new MatchNeighborsData { radiusDisplay = GetEntity( auth.display, TransformUsageFlags.Dynamic ), radius = auth.radius } );
			AddComponent<MatchingNeighbors>( self ); SetComponentEnabled<MatchingNeighbors>( self, false );
			AddComponent<PreventMatchingNeighborsThisStep>( self ); SetComponentEnabled<PreventMatchingNeighborsThisStep>( self, false );
		}
	}
}

public struct MatchNeighborsData : IComponentData {
	public Entity radiusDisplay;
	public float radius;
}

public struct MatchingNeighbors : IComponentData, IEnableableComponent {}

public struct PreventMatchingNeighborsThisStep : IComponentData, IEnableableComponent {}