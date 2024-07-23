using Unity.Entities;
using UnityEngine;

public class StartAnimation : MonoBehaviour {
	public float startDist;
	public float endDist;
	public float duration;
	public bool startGameAfterCompletion; // turn this into a generalized component when you have time
	
	public SharedCurve easing;

	public class Baker : Baker<StartAnimation> {

		public override void Bake( StartAnimation auth ) {
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.easing );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StartAnimData {
				startDist = auth.startDist,
				endDist = auth.endDist,
				duration = auth.duration,
				startGameAfterCompletion = auth.startGameAfterCompletion,
				easing = blobAssetRef
			} );
		}
	}
}

public struct StartAnimData : IComponentData {
	public float timeElapsed;
	public float duration;

	public float startDist;
	public float endDist;

	public bool startGameAfterCompletion;
	
	public BlobAssetReference<CurveBlob> easing;
}