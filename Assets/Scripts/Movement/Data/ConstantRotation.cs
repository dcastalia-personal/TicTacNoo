using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ConstantRotation : MonoBehaviour {
	public Vector3 rotPerSecond;

	public class Baker : Baker<ConstantRotation> {

		public override void Bake( ConstantRotation auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ConstantRotationData { rotPerSecond = math.radians( auth.rotPerSecond ) } );
		}
	}
}

public struct ConstantRotationData : IComponentData {
	public float3 rotPerSecond;
}