using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Path))]
public class SplinePath : MonoBehaviour {
	public Path path;
	public int resolution;

	void OnValidate() {
		path = GetComponent<Path>();
	}
}

public struct SplineBlob {
    public BlobArray<float3> pts;
    public float distance;
    
    public static BlobAssetReference<SplineBlob> CreateSplineBlobAssetRef( Path path, int resolution ) {
	    var pathPts = path.ToNativeArray();
	    
	    pathPts.ToSpline( resolution, out NativeArray<float3> splinePts );

      using var blobBuilder = new BlobBuilder(Allocator.Temp);
      ref var splineRoot = ref blobBuilder.ConstructRoot<SplineBlob>();

      var ptsBuilder = blobBuilder.Allocate(ref splineRoot.pts, splinePts.Length);
      for( int i = 0; i < splinePts.Length; i++ ) ptsBuilder[i] = splinePts[i];

      splineRoot.distance = splinePts.Distance();

      return blobBuilder.CreateBlobAssetReference<SplineBlob>( Allocator.Persistent );
    }

    public NativeArray<float3> ToNativeArray() {
	    var array = new NativeArray<float3>( pts.Length, Allocator.Temp );
	    for( int index = 0; index < pts.Length; index++ ) array[ index ] = pts[ index ];
	    return array;
    }
}