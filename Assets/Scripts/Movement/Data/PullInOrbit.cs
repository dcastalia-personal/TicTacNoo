using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

public class PullInOrbit : MonoBehaviour {
	public float speed;
	public float range;
	public float gravity;
	public float rotSpeed;
	public SharedCurve falloff;
	
	public PhysicsCategoryTags physicsBelongTo;
	public PhysicsCategoryTags physicsCollideWith;

	public class Baker : Baker<PullInOrbit> {

		public override void Bake( PullInOrbit auth ) {
			if( !auth.falloff ) return;
			
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.falloff );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new PullInOrbitData {
				speed = auth.speed,
				rotSpeed = auth.rotSpeed,
				range = auth.range,
				gravity = auth.gravity,
				falloff = blobAssetRef,
				physicsFilter = new CollisionFilter { BelongsTo = auth.physicsBelongTo.Value, CollidesWith = auth.physicsCollideWith.Value }
			} ); 
			SetComponentEnabled<PullInOrbitData>( self, false );
		}
	}
}

public struct PullInOrbitData : IComponentData, IEnableableComponent {
	public float speed;
	public float range;
	public float gravity;
	public BlobAssetReference<CurveBlob> falloff;
	public float3 axisOfRotation; // when the shape is first activated, use the camera's forward vector at that time as the persistent axis of rotation
	public float rotSpeed;
	
	public CollisionFilter physicsFilter;
}