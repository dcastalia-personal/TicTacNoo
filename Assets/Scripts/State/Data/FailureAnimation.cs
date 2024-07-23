using Unity.Entities;
using UnityEngine;

public class FailureAnimation : MonoBehaviour {
	public float startMoveSpeed;
	public float startRotSpeed;
	public float acceleration;
	public float angAcceleration;
	public float shapesOutDuration;
	public float nextLevelStartDuration;

	public class Baker : Baker<FailureAnimation> {

		public override void Bake( FailureAnimation auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FailureAnimData {
				moveSpeed = auth.startMoveSpeed, 
				rotSpeed = auth.startRotSpeed, 
				acceleration = auth.acceleration, 
				angAcceleration = auth.angAcceleration, 
				shapesOutDuration = auth.shapesOutDuration,
				nextLevelStartDuration = auth.nextLevelStartDuration,
			} );
		}
	}
}

public struct FailureAnimData : IComponentData {
	public float timeElapsed;
	public float shapesOutDuration;
	public float nextLevelStartDuration;
	public float moveSpeed;
	public float rotSpeed; // in radians
	public float acceleration;
	public float angAcceleration;
}