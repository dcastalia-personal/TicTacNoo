using System;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Waypoint : MonoBehaviour {
#if UNITY_EDITOR
	void OnDestroy() {
		if( Application.isPlaying ) return;
		var path = GetComponentInParent<Path>();
		if( !path ) return;
		if( path.waypoints == null ) return;
		path.waypoints = path.waypoints.Where( waypoint => waypoint != null ).ToList();
	}
#endif
}
