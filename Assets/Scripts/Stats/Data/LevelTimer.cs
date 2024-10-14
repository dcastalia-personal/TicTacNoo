using Unity.Entities;
using UnityEngine;

public class LevelTimer : MonoBehaviour {

	public class Baker : Baker<LevelTimer> {

		public override void Bake( LevelTimer auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LevelTimerData {} );
		}
	}
}

public struct LevelTimerData : IComponentData {
	public float value;
}