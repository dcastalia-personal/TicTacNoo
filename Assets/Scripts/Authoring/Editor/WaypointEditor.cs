using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Waypoint))]
public class WaypointEditor : Editor
{
	public override VisualElement CreateInspectorGUI() {
		VisualElement rootElement = new VisualElement();
		InspectorElement.FillDefaultInspector( rootElement, serializedObject, this );

		var button = new Button( CreateNewWaypoint ) {
			text = "Create New Waypoint"
		};

		rootElement.Add( button );

		return rootElement;
	}

	void CreateNewWaypoint() {
		var waypoint = target as Waypoint;
		var path = waypoint.GetComponentInParent<Path>();

		PathEditor.AddPathPt( path );
	}
}
