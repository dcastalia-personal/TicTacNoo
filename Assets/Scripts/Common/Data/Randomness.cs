using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Randomness : MonoBehaviour {

	public class Baker : Baker<Randomness> {

		public override void Bake( Randomness auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RandomnessData { rng = new Random( (uint)UnityEngine.Random.Range( 0, int.MaxValue ) ) } );
		}
	}
}

public struct RandomnessData : IComponentData {
	public Random rng;
}