using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FollowPath : MonoBehaviour {
	public GameObject path;
	public float speed;

	public class Baker : Baker<FollowPath> {

		public override void Bake( FollowPath auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FollowPathData { pathToFollow = GetEntity( auth.path, TransformUsageFlags.Dynamic ), speed = auth.speed } );
			AddComponent( self, new Following() ); SetComponentEnabled<Following>( self, false );
		}
	}
}

public struct FollowPathData : IComponentData {
	public Entity pathToFollow;
	public int curWaypointIndex;
	public int nextWaypointIndex;

	public float3 curWaypointPos;
	public float3 nextWaypointPos;

	public float speed;
}

public struct Following : IComponentData, IEnableableComponent {}