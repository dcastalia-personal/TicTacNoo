using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

public class FollowSpline : MonoBehaviour {
	public SplinePath splinePath;
	public float speed;

	public class Baker : Baker<FollowSpline> {

		public override void Bake( FollowSpline auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );

			var nativeSplineBlobAssetRef = SplineBlob.CreateSplineBlobAssetRef( auth.splinePath.path, auth.splinePath.resolution );
			AddBlobAsset( ref nativeSplineBlobAssetRef, out _ );
			
			AddComponent( self, new FollowSplineData { spline = nativeSplineBlobAssetRef, speed = auth.speed } );
			AddComponent( self, new Following() ); SetComponentEnabled<Following>( self, false );
		}
	}
}

public struct FollowSplineData : IComponentData {
	public BlobAssetReference<SplineBlob> spline;
	public float speed;
	public float timeElapsed;
}