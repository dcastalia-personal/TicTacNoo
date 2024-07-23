using Unity.Entities;
using UnityEngine;

public class FollowContinuously : MonoBehaviour {

	public class Baker : Baker<FollowContinuously> {

		public override void Bake( FollowContinuously auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FollowContinuouslyData {} );
			AddComponent( self, new FinishedFollowing() ); SetComponentEnabled<FinishedFollowing>( self, false );
		}
	}
}

public struct FollowContinuouslyData : IComponentData {
	public float elapsedTime;
}

public struct FinishedFollowing : IComponentData, IEnableableComponent {
	
}