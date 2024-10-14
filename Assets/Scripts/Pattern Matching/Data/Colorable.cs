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
			AddComponent( self, new MatchableColor {} );
			AddComponent( self, new MatchableColorChanged() ); SetComponentEnabled<MatchableColorChanged>( self, false );
		}
	}
}

public struct MatchableColor : IComponentData {
	public float4 value;
	public float emission;
} // an instant declaration of color that can be used for matching independently of fade level
public struct MatchableColorChanged : IComponentData, IEnableableComponent {}

[MaterialProperty("_PrimaryColor")] public struct CurrentColorData : IComponentData { public float4 value; }
[MaterialProperty("_Emission")] public struct CurrentEmissionData : IComponentData { public float value; }