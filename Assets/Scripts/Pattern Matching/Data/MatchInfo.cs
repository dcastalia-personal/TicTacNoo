using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MatchInfo : MonoBehaviour {
	public int criticalMass;
	public float matchDistance;

	public class Baker : Baker<MatchInfo> {

		public override void Bake( MatchInfo auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchInfoData { criticalMass = auth.criticalMass, matchDistance = auth.matchDistance } );
		}
	}
}

public struct MatchInfoData : IComponentData {
	public int criticalMass;
	public float matchDistance;
	public const float matchedColorIntensity = 10f;
}