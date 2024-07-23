using Unity.Entities;
using UnityEngine;

public class GameState : MonoBehaviour {
	public GameObject startAnimationPrefab;
	public GameObject inGamePrefab;
	public GameObject successAnimationPrefab;
	public GameObject failureAnimationPrefab;
	public GameObject failureAckPrefab;
	public GameObject successAckPrefab;
	
	public int startLevel;

	public float stepDuration;

	public class Baker : Baker<GameState> {

		public override void Bake( GameState auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new GameStateData {
				startLevel = auth.startLevel,
				startAnimationPrefab = GetEntity( auth.startAnimationPrefab, TransformUsageFlags.None ),
				successAnimationPrefab = GetEntity( auth.successAnimationPrefab, TransformUsageFlags.None ), 
				failureAnimationPrefab = GetEntity( auth.failureAnimationPrefab, TransformUsageFlags.None ),
				failureAckPrefab = GetEntity( auth.failureAckPrefab, TransformUsageFlags.None ),
				successAckPrefab = GetEntity( auth.successAckPrefab, TransformUsageFlags.None ),
				inGamePrefab = GetEntity( auth.inGamePrefab, TransformUsageFlags.None ),
			} );

			AddComponent( self, new PlayerStepTag() );
			AddComponent( self, new PlayerStepped() ); SetComponentEnabled<PlayerStepped>( self, false );
			AddComponent( self, new PlayerStepData { duration = auth.stepDuration } ); SetComponentEnabled<PlayerStepData>( self, false );
			AddComponent( self, new PlayerFinishedStepping {} ); SetComponentEnabled<PlayerFinishedStepping>( self, false );
		}
	}
}

public struct GameStateData : IComponentData {
	public int curLevel;
	public int nextLevel;
	public int startLevel;
	
	public Entity startAnimationPrefab;
	public Entity inGamePrefab;
	public Entity successAnimationPrefab;
	public Entity failureAnimationPrefab;
	public Entity successAckPrefab;
	public Entity failureAckPrefab;
}

public struct SwitchLevel : IComponentData {}

public struct PlayerStepTag : IComponentData {}
public struct PlayerStepped : IComponentData, IEnableableComponent {}
public struct PlayerStepData : IComponentData, IEnableableComponent {
	public float duration;
	public float time;
}

public struct PlayerFinishedStepping : IComponentData, IEnableableComponent {}