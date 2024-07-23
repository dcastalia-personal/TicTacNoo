using Unity.Entities;
using UnityEngine;

public class FollowOnStep : MonoBehaviour {

	public class Baker : Baker<FollowOnStep> {

		public override void Bake( FollowOnStep auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FollowOnStepData {} );
		}
	}
}

public struct FollowOnStepData : IComponentData {}