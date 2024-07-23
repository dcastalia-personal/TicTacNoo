using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SharedCurve : MonoBehaviour {
	public AnimationCurve curve;
	public int samplingResolution;
}

public struct CurveBlob {
	public BlobArray<float3> pts;
    
	public static BlobAssetReference<CurveBlob> CreateCurveBlob( SharedCurve sharedCurve ) {
		var lastKey = sharedCurve.curve.keys.Last();
		var maxTime = lastKey.time;

		var sampledPts = new NativeArray<float3>( (int)(sharedCurve.samplingResolution / maxTime) + 1, Allocator.Temp );

		var timeStep = maxTime / sharedCurve.samplingResolution;

		var time = 0f;
		for( int index = 0; index < sampledPts.Length - 1; index++ ) {
			sampledPts[ index ] = new float3( time, sharedCurve.curve.Evaluate( time ), 0f );
			time += timeStep;
		}

		sampledPts[ ^1 ] = new float3( lastKey.time, lastKey.value, 0f );

		using var blobBuilder = new BlobBuilder(Allocator.Temp);
		ref var root = ref blobBuilder.ConstructRoot<SplineBlob>();

		var ptsBuilder = blobBuilder.Allocate(ref root.pts, sampledPts.Length);
		for( int i = 0; i < sampledPts.Length; i++ ) ptsBuilder[i] = sampledPts[i];

		return blobBuilder.CreateBlobAssetReference<CurveBlob>( Allocator.Persistent );
	}

	public float Sample( float time ) {
		pts.Sample( time, out float3 value );

		return value.y;
	}
}