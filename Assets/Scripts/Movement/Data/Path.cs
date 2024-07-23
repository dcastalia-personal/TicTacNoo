using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Path : MonoBehaviour {
	public List<GameObject> waypoints = new();

	public class Baker : Baker<Path> {

		public override void Bake( Path auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			var positions = AddBuffer<PathData>( self );

			foreach( var waypoint in auth.waypoints ) {
				positions.Add( new PathData { position = waypoint.transform.position } );
			}
		}
	}

	public NativeArray<float3> ToNativeArray() {
		var array = new NativeArray<float3>( waypoints.Count, Allocator.Temp );
		for( int index = 0; index < waypoints.Count; index++ ) array[ index ] = waypoints[ index ].transform.position;
		return array;
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.green;
		if( waypoints.Count < 2 ) return;
		
		for( int index = 0; index < waypoints.Count; index++ ) {
			var nextIndex = (index + 1) % waypoints.Count;

			Gizmos.DrawLine( waypoints[ index ].transform.position, waypoints[ nextIndex ].transform.position );
		}
	}
}

public struct PathData : IBufferElementData {
	public float3 position;
}