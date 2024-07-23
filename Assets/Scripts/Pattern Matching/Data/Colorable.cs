using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class Colorable : MonoBehaviour {

	public class Baker : Baker<Colorable> {

		public override void Bake( Colorable auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new CurrentColorData {} );
			AddComponent( self, new CurrentEmissionData {} );
		}
	}
}

[MaterialProperty("_PrimaryColor")] public struct CurrentColorData : IComponentData { public float4 value; }
[MaterialProperty("_Emission")] public struct CurrentEmissionData : IComponentData { public float value; }