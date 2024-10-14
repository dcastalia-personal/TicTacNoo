using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(RequireInit))]
public class RandomAnimModifier : MonoBehaviour {

	public class Baker : Baker<RandomAnimModifier> {

		public override void Bake( RandomAnimModifier auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new RandomAnimModifierData { min = 1f, max = 1.5f } );
		}
	}
}

public struct RandomAnimModifierData : IComponentData {
	public float value;
	public float min;
	public float max;
	public float3 randomDir;
}