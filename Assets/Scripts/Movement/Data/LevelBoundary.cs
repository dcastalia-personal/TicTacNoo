using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class LevelBoundary : MonoBehaviour {
	public StartAnimation startAnimation;

	public class Baker : Baker<LevelBoundary> {

		public override void Bake( LevelBoundary auth ) {
			if( auth.startAnimation ) auth.transform.localScale = new Vector3( auth.startAnimation.endDist, auth.startAnimation.endDist, auth.startAnimation.endDist );
		}
	}
}

public struct LevelBoundaryData : IComponentData {
	
}