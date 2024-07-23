using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class Pressable : MonoBehaviour {

	public class Baker : Baker<Pressable> {

		public override void Bake( Pressable auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Pressed{} ); SetComponentEnabled<Pressed>( self, false );
			AddComponent( self, new Released{} ); SetComponentEnabled<Pressed>( self, false );
			AddComponent( self, new Held{} ); SetComponentEnabled<Held>( self, false );
		}
	}
}

public struct Pressed : IComponentData, IEnableableComponent {
	public float3 worldPos;
}

public struct Released : IComponentData, IEnableableComponent {
	public float3 worldPos;
}

public struct Held : IComponentData, IEnableableComponent {
	public float3 worldPos;
}