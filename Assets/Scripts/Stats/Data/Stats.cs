using System;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class Stats : MonoBehaviour {
	public GameState gameState;

	#if UNITY_EDITOR
	public class Baker : Baker<Stats> {

		public override void Bake( Stats auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			var statBuffer = AddBuffer<StatData>( self );

			for( int index = 0; index < auth.gameState.levels.Count; index++ ) {
				statBuffer.Add( new StatData {} );
			}

			AddComponent<LoadTag>( self );
		}
	}
	
	#endif
}

[Serializable]
public struct StatData : IBufferElementData {
	public float lowestTime;
}

[Serializable]
public class StatsData {
	public StatData[] elements;
}