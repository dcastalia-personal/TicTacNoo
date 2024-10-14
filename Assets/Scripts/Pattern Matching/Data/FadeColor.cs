using Unity.Entities;
using UnityEngine;

public class FadeColor : MonoBehaviour {
	public float speed;
	public SharedCurve easing;

	public class Baker : Baker<FadeColor> {

		public override void Bake( FadeColor auth ) {
			if( !auth.easing ) return;
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.easing );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FadeColorData { speed = auth.speed, easing = blobAssetRef} );
		}
	}
}

public struct FadeColorData : IComponentData, IEnableableComponent {
	public float speed;
	public float time;
	public BlobAssetReference<CurveBlob> easing;
}