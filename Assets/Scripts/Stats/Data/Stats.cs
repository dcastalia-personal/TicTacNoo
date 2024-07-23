using Unity.Entities;
using UnityEngine;

public class Stats : MonoBehaviour {

	public class Baker : Baker<Stats> {

		public override void Bake( Stats auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new StatsData {} );
		}
	}
}

public struct StatsData : IComponentData {
	public int highestLevelAchieved;
	public float totalSecondsOnRun;
	public float averageSecondsToCompleteLevel;
}