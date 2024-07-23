using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TargetColor : MonoBehaviour {
	public Color defaultColor;
	public float tweenSpeed;

	public class Baker : Baker<TargetColor> {

		public override void Bake( TargetColor auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new TargetColorData { baseColor = (Vector4)auth.defaultColor, defaultColor = (Vector4)auth.defaultColor, tweenSpeed = auth.tweenSpeed } );
			AddComponent( self, new TargetColorDataChanged() ); SetComponentEnabled<TargetColorDataChanged>( self, false );
		}
	}
}

public struct TargetColorData : IComponentData, IEnableableComponent {
	public float4 baseColor;
	public float4 defaultColor;
	public float emission;
	public float tweenSpeed;
}

public struct TargetColorDataChanged : IComponentData, IEnableableComponent {}