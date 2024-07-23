using Unity.Entities;
using UnityEngine;

public class SpawnAtInterval : MonoBehaviour {
	public GameObject prefabToSpawn;
	public float interval;
	public int maxSpawnCount;

	public class Baker : Baker<SpawnAtInterval> {

		public override void Bake( SpawnAtInterval auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new SpawnAtIntervalData { interval = auth.interval, prefabToSpawn = GetEntity( auth.prefabToSpawn, TransformUsageFlags.Dynamic ), maxSpawnCount = auth.maxSpawnCount } );
		}
	}
}

public struct SpawnAtIntervalData : IComponentData {
	public Entity prefabToSpawn;
	
	public float interval;
	public float time;

	public int maxSpawnCount;
	public int curSpawnCount;
}