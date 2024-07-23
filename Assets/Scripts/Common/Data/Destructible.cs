using Unity.Entities;
using UnityEngine;

public class Destructible : MonoBehaviour {

	public class Baker : Baker<Destructible> {

		public override void Bake( Destructible auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ShouldDestroy {} ); SetComponentEnabled<ShouldDestroy>( self, false );
		}
	}
}

public struct ShouldDestroy : IComponentData, IEnableableComponent {}