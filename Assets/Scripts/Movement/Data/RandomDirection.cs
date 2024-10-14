using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RandomDirection : MonoBehaviour {
	public Vector3 modifiers;
	public float lengthMin = 1f;
	public float lengthMax = 1f;

	public class Baker : Baker<RandomDirection> {

		public override void Bake( RandomDirection auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RandomDirectionData { modifiers = auth.modifiers, lengthMin = auth.lengthMin, lengthMax = auth.lengthMax } );
		}
	}
}

public struct RandomDirectionData : IComponentData {
	public float3 modifiers;
	public float3 value;

	public float lengthMin;
	public float lengthMax;
}