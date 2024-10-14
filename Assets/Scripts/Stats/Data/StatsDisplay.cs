using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class StatsDisplay : MonoBehaviour {
	public VisualTreeAsset statsEntryPrefab;
	public GameObject music;

	public class Baker : Baker<StatsDisplay> {

		public override void Bake( StatsDisplay auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StatsDisplayData { statsEntryPrefab = new UnityObjectRef<VisualTreeAsset> { Value = auth.statsEntryPrefab }, music = GetEntity( auth.music, TransformUsageFlags.Dynamic ) } );
		}
	}
}

public struct StatsDisplayData : IComponentData {
	public UnityObjectRef<UIDocument> document;
	public UnityObjectRef<VisualTreeAsset> statsEntryPrefab;
	public Entity music;
	public Entity musicInstance;
}