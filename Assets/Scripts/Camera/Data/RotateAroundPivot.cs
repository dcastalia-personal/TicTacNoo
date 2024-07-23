using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateAroundPivot : MonoBehaviour {
	public float speed;

	public class Baker : Baker<RotateAroundPivot> {

		public override void Bake( RotateAroundPivot auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new RotateAroundPivotData {
				speed = auth.speed,
			} );
		}
	}
}

public struct RotateAroundPivotData : IComponentData {
	public float speed;
}