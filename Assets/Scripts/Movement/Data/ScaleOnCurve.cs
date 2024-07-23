using Unity.Entities;
using UnityEngine;

public class ScaleOnCurve : MonoBehaviour {
	public SharedCurve curve;
	public float speed;

	public FinishMode finishMode;

	public class Baker : Baker<ScaleOnCurve> {

		public override void Bake( ScaleOnCurve auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );

			if( !auth.curve ) return;
			
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.curve );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			AddComponent( self, new ScaleOnCurveData { curve = blobAssetRef, speed = auth.speed, finishMode = auth.finishMode} );
			SetComponentEnabled<ScaleOnCurveData>( self, false );
		}
	}
}

public struct ScaleOnCurveData : IComponentData, IEnableableComponent {
	public BlobAssetReference<CurveBlob> curve;
	public float speed;

	public float elapsedTime;

	public FinishMode finishMode;
}