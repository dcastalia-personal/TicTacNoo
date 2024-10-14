using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

public class BridgeBeam : MonoBehaviour {
	public PhysicsCategoryTags beamBelongTo;
	public PhysicsCategoryTags beamCollidesWith;

	public class Baker : Baker<BridgeBeam> {

		public override void Bake( BridgeBeam auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new BridgeBeamData { collisionFilter = new CollisionFilter { BelongsTo = auth.beamBelongTo.Value, CollidesWith = auth.beamCollidesWith.Value }} );
			AddComponent( self, new LaserLength {} );
			AddComponent( self, new CurrentColorData {} );
		}
	}
}

public struct BridgeBeamData : IComponentData {
	public Entity start;
	public Entity end;

	public CollisionFilter collisionFilter;
}