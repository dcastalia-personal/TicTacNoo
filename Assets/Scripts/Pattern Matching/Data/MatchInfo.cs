using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MatchInfo : MonoBehaviour {
	public int criticalMass;
	public float matchDistance;
	public float stepDuration;

	public class Baker : Baker<MatchInfo> {

		public override void Bake( MatchInfo auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchInfoData { criticalMass = auth.criticalMass, matchDistance = auth.matchDistance } );
			
			AddComponent( self, new PlayerStepTag() );
			AddComponent( self, new PlayerStepped() ); SetComponentEnabled<PlayerStepped>( self, false );
			AddComponent( self, new PlayerStepData { duration = auth.stepDuration } ); SetComponentEnabled<PlayerStepData>( self, false );
			AddComponent( self, new PlayerFinishedStepping {} ); SetComponentEnabled<PlayerFinishedStepping>( self, false );
		}
	}
}

public struct MatchInfoData : IComponentData {
	public int criticalMass;
	public float matchDistance;
	public const float matchedColorIntensity = 10f;
}

public struct PlayerStepTag : IComponentData {}
public struct PlayerStepped : IComponentData, IEnableableComponent {}
public struct PlayerStepData : IComponentData, IEnableableComponent {
	public float duration;
	public float time;
}

public struct PlayerFinishedStepping : IComponentData, IEnableableComponent {}