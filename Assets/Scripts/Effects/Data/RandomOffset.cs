using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class RandomOffset : MonoBehaviour {
	public float min;
	public float max;

	public class Baker : Baker<RandomOffset> {

		public override void Bake( RandomOffset auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RandomOffsetData { value = Random.Range( auth.min, auth.max ) } );
		}
	}
}

[MaterialProperty("_Random_Offset")] public struct RandomOffsetData : IComponentData { public float value; }