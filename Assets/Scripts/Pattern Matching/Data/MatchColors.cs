using Unity.Entities;
using UnityEngine;

public class MatchColors : MonoBehaviour {

	public class Baker : Baker<MatchColors> {

		public override void Bake( MatchColors auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchColorsData {} );
		}
	}
}

public struct MatchColorsData : IComponentData {}