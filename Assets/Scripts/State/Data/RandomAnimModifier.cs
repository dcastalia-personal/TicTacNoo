using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomAnimModifier : MonoBehaviour {
	public float min;
	public float max;

	public class Baker : Baker<RandomAnimModifier> {

		public override void Bake( RandomAnimModifier auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new RandomAnimModifierData { value = Random.Range( auth.min, auth.max ), randomDir = Random.onUnitSphere } );
		}
	}
}

public struct RandomAnimModifierData : IComponentData {
	public float value;
	public float3 randomDir;
}