using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public static class Spline {

	[BurstCompile]
	public static void ToSpline( this in NativeArray<float3> inputPoints, float stepLength, out NativeArray<float3> interpolatedPositions ) {
		// add control points
		var pts = new NativeArray<float3>( inputPoints.Length + 2, Allocator.Temp );
		for( int index = 1; index < pts.Length - 1; index++ ) pts[ index ] = inputPoints[ index - 1 ];
		pts[ 0 ] = pts[ 1 ];
		pts[ ^1 ] = pts[ ^2 ];

		// broadly approximate distance
		float approxDist = 0f;

		for( int index = 1; index < inputPoints.Length; index++ ) {
			approxDist += math.distance( inputPoints[ index - 1 ], inputPoints[ index ] );
		}

		// determine segment length and sample the curve at those intervals
		interpolatedPositions = new NativeArray<float3>( (int)(approxDist / stepLength), Allocator.Temp );

		float normalizedSegLength = stepLength / approxDist;

		for( int index = 0; index < interpolatedPositions.Length; index++ ) {
			Sample( pts, (float)index * normalizedSegLength, out float3 result );
			interpolatedPositions[ index ] = result;
		}
	}

	[BurstCompile]
	public static void ToSpline( this in NativeArray<float3> inputPoints, int resultLength, out NativeArray<float3> outputPoints ) {
		outputPoints = new NativeArray<float3>( inputPoints.Length + 2, Allocator.Temp );
		for( int index = 1; index < outputPoints.Length - 1; index++ ) outputPoints[ index ] = inputPoints[ index - 1 ];
		outputPoints[ 0 ] = outputPoints[ 1 ];
		outputPoints[ ^1 ] = outputPoints[ ^2 ];

		var interpolatedPositions = new NativeArray<float3>( resultLength, Allocator.Temp );
		float interpolatedT = 1f / (float)(resultLength - 1);

		for( int index = 0; index < interpolatedPositions.Length; index++ ) {
			 Sample( outputPoints, (float)index * interpolatedT, out float3 result );
			 interpolatedPositions[ index ] = result;
		}
	}

	[BurstCompile]
	public static void Sample( this in NativeArray<float3> pts, float t, out float3 val ) {
		switch( t ) {
			case >= 1f:
				val = pts[ ^1 ];
				return;
			case <= 0f:
				val = pts[ 0 ];
				return;
		}
		
		int numSections = pts.Length - 3;
		var curPt = Mathf.Min( Mathf.FloorToInt( t * (float)numSections ), numSections - 1 );
		float u = t * (float)numSections - (float)curPt;

		float3 a = pts[ curPt ];
		float3 b = pts[ curPt + 1 ];
		float3 c = pts[ curPt + 2 ];
		float3 d = pts[ curPt + 3 ];

		val = (.5f
		       * (
			       (-a + 3f * b - 3f * c + d) * (u * u * u)
			       + (2f * a - 5f * b + 4f * c - d) * (u * u)
			       + (-a + c) * u
			       + 2f * b));
	}
	
	[BurstCompile]
	public static void Sample( this ref BlobArray<float3> pts, float t, out float3 val ) {
		switch( t ) {
			case >= 1f:
				val = pts[ ^1 ];
				return;
			case <= 0f:
				val = pts[ 0 ];
				return;
		}

		int numSections = pts.Length - 3;
		var curPt = Mathf.Min( Mathf.FloorToInt( t * (float)numSections ), numSections - 1 );
		float u = t * (float)numSections - (float)curPt;

		float3 a = pts[ curPt ];
		float3 b = pts[ curPt + 1 ];
		float3 c = pts[ curPt + 2 ];
		float3 d = pts[ curPt + 3 ];

		val = (.5f
		       * (
			       (-a + 3f * b - 3f * c + d) * (u * u * u)
			       + (2f * a - 5f * b + 4f * c - d) * (u * u)
			       + (-a + c) * u
			       + 2f * b));
	}
}