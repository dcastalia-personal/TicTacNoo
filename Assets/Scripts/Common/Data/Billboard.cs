using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Billboard : MonoBehaviour {

	public class Baker : Baker<Billboard> {

		public override void Bake( Billboard auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new BillboardData {} );
		}
	}
}

public struct BillboardData : IComponentData {}