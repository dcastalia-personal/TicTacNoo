using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile] public static class MathExtensions {
	
	[BurstCompile] public static float Distance( this in NativeArray<float3> pts ) {
		float distance = 0f;

		for( int index = 0; index < pts.Length - 1; index++ ) {
			distance += math.distance( pts[ index ], pts[ index + 1 ] );
		}

		return distance;
	}
	
	[BurstCompile] public static float Distance( this in NativeArray<float3> pts, int toIndex ) {
		float distance = 0f;

		for( int index = 0; index < toIndex - 1; index++ ) {
			distance += math.distance( pts[ index ], pts[ index + 1 ] );
		}

		return distance;
	}

	[BurstCompile] public static void GetClosestPt( this in NativeArray<float3> pts, in float3 inPt, out float3 outPt, out int outIndex ) {
		// Debug.Log( $"Looking at {pts.Length} based on in position {inPt}" );
		float closestDistSq = float.MaxValue;
		outIndex = 0;

		for( int index = 0; index < pts.Length; index++ ) {
			float3 pointCandidate = pts[ index ];
			var distSq = math.distancesq( inPt, pointCandidate );

			if( distSq < closestDistSq ) {
				// Debug.Log( $"Dist {distSq} from {inPt} to pt index {index} at {pts[index]} is less than the current minimum of {closestDistSq}, so setting out index to {index}" );
				closestDistSq = distSq;
				outIndex = index;
			}
			// else {
			// 	Debug.Log( $"Dist {distSq} from {inPt} to pt index {index} at {pts[index]} is NOT less than the current minimum of {closestDistSq}" );
			// }
		}

		outPt = pts[outIndex];
	}
}