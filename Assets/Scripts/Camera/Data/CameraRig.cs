using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraRig : MonoBehaviour {
	public bool orthographic;
	public float orthoSize;

	public class Baker : Baker<CameraRig> {

		public override void Bake( CameraRig auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new CameraData { orthographic = auth.orthographic, orthoSize = auth.orthoSize } );
			AddComponent( self, new TargetColorData() ); SetComponentEnabled<TargetColorData>( self, false );
		}
	}
}

public struct CameraData : IComponentData {
	public UnityObjectRef<Camera> camera;
	public float orthoSize;
	public float aspect;
	public bool orthographic;
}