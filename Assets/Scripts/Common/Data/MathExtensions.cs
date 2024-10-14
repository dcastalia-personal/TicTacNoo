using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile] public static class MathExtensions {
	
	[BurstCompile]
	public static float Angle( in float3 from, in float3 to ) => math.acos( math.clamp( math.dot( from, to ), -1f, 1f ) );
	
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
		float closestDistSq = float.MaxValue;
		outIndex = 0;

		for( int index = 0; index < pts.Length; index++ ) {
			float3 pointCandidate = pts[ index ];
			var distSq = math.distancesq( inPt, pointCandidate );

			if( distSq < closestDistSq ) {
				closestDistSq = distSq;
				outIndex = index;
			}
		}

		outPt = pts[outIndex];
	}
	
	// public static float3 MultiplyPoint( in float4x4 matrix, in float3 point ) {
	// 	return math.mul( matrix, new float4( point, 1 ) ).xyz;
	// }
	//
	// [BurstCompile] public static float3 ScreenToWorldPoint( in float4x4 projectionMatrix, in float4x4 worldToCameraMatrix, in float4x4 localToWorldMatrix, in float3 screenPos ) {
	// 	var worldToScreen = math.mul( math.mul( projectionMatrix, worldToCameraMatrix ), localToWorldMatrix );
	// 	var screenToWorld = math.inverse( math.mul( projectionMatrix, worldToCameraMatrix ) );
	// 	var depth = MultiplyPoint( worldToScreen, screenPos ).z;
	// 	var viewPos = new float3( screenPos.x / Screen.width, screenPos.y / Screen.height, (depth + 1f) / 2f );
	// 	var clipPos = viewPos * 2f - new float3(1);
	// 	return MultiplyPoint( screenToWorld, clipPos );
	// }
}