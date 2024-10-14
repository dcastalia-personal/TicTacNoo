using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TargetColor : MonoBehaviour {
	public float tweenSpeed;

	public class Baker : Baker<TargetColor> {

		public override void Bake( TargetColor auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new TargetColorData { tweenSpeed = auth.tweenSpeed } );
		}
	}
}

public struct TargetColorData : IComponentData, IEnableableComponent {
	public float4 baseColor;
	public float emission;
	public float tweenSpeed;
}