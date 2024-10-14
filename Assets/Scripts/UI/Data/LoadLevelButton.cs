using Unity.Entities;
using UnityEngine;

public class LoadLevelButton : MonoBehaviour {
	public int levelToLoad;

	public class Baker : Baker<LoadLevelButton> {

		public override void Bake( LoadLevelButton auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LoadLevelButtonData { levelToLoad = auth.levelToLoad } );
		}
	}
}

public struct LoadLevelButtonData : IComponentData {
	public int levelToLoad;
}