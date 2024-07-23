using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[CustomEditor(typeof(Path))]
public class PathEditor : Editor
{
	public override VisualElement CreateInspectorGUI() {
		VisualElement rootElement = new();
		InspectorElement.FillDefaultInspector( rootElement, serializedObject, this );

		var addButton = new Button( () => AddPathPt( target as Path ) ) {
			text = "Add Path Point"
		};
		
		var removeButton = new Button( () => RemovePathPt( target as Path ) ) {
			text = "Remove Path Point"
		};

		rootElement.Add( addButton );
		rootElement.Add( removeButton );

		return rootElement;
	}

	public static void AddPathPt( Path path ) {
		var mostRecentWaypoint = path.waypoints.LastOrDefault();
		if( !mostRecentWaypoint ) return;

		var waypointPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( mostRecentWaypoint.gameObject );
		var waypointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( waypointPath );

		var nextWaypoint = PrefabUtility.InstantiatePrefab( waypointPrefab, path.transform ) as GameObject;
		nextWaypoint.transform.position = mostRecentWaypoint.transform.position;
		nextWaypoint.name = mostRecentWaypoint.name;
		path.waypoints.Add( nextWaypoint );

		Selection.activeGameObject = nextWaypoint.gameObject;

		EditorUtility.SetDirty( path );
		EditorSceneManager.MarkSceneDirty( SceneManager.GetActiveScene() );
	}

	public static void RemovePathPt( Path path ) {
		var mostRecentWaypoint = path.waypoints.LastOrDefault();
		if( !mostRecentWaypoint ) return;
		
		DestroyImmediate( mostRecentWaypoint.gameObject );
		path.waypoints.RemoveAt( path.waypoints.Count - 1 );

		EditorUtility.SetDirty( path );
		EditorSceneManager.MarkSceneDirty( SceneManager.GetActiveScene() );
	}
}
