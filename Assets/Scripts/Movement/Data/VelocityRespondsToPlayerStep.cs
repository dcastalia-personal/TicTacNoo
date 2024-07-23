using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class VelocityRespondsToPlayerStep : MonoBehaviour {

	public class Baker : Baker<VelocityRespondsToPlayerStep> {

		public override void Bake( VelocityRespondsToPlayerStep auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new VelocityRespondsToPlayerStepData {} );
		}
	}
}

public struct VelocityRespondsToPlayerStepData : IComponentData {
	public float3 linearVelocity;
	public float3 angularVelocity;
}