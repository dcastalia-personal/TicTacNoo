using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Jitter : MonoBehaviour {
	public float power;
	public float speed;
	public float acceleration;
	public Vector3 noiseEntryPointsPerAxis;
	public SharedCurve easing;

	public class Baker : Baker<Jitter> {

		public override void Bake( Jitter auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );

			if( !auth.easing ) return;
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.easing );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			AddComponent( self, new JitterData { power = auth.power, speed = auth.speed, noiseEntryPointsPerAxis = auth.noiseEntryPointsPerAxis, acceleration = auth.acceleration, easing = blobAssetRef} );
			SetComponentEnabled<JitterData>( self, false );
		}
	}
}

public struct JitterData : IComponentData, IEnableableComponent {
	public float power;
	public float speed;
	public float3 noiseEntryPointsPerAxis;
	public float acceleration;
	
	public float time;
	public float intensity;

	public BlobAssetReference<CurveBlob> easing;
}