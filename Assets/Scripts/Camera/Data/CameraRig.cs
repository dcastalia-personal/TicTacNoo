using Unity.Entities;
using UnityEngine;

public class CameraRig : MonoBehaviour {

	public class Baker : Baker<CameraRig> {

		public override void Bake( CameraRig auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new CameraData {} );
			AddComponent( self, new TargetColorData() ); SetComponentEnabled<TargetColorData>( self, false );
		}
	}
}

public struct CameraData : IComponentData {
	public UnityObjectRef<Camera> camera;
}