using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class StoryAnimation : MonoBehaviour {
	public int nextLevel;

	public class Baker : Baker<StoryAnimation> {

		public override void Bake( StoryAnimation auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StoryAnimationData { nextLevel = auth.nextLevel} );
		}
	}
}

public struct StoryAnimationData : IComponentData {
	public UnityObjectRef<UIDocument> storyUI;

	public int stages;
	public int curStage;
	public int nextLevel;
}