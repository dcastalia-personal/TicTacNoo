using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class LevelColors : MonoBehaviour {
	public ColorWithIntensity[] colors;

	public class Baker : Baker<LevelColors> {

		public override void Bake( LevelColors auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LevelColorIndex() );

			var colorsBuffer = AddBuffer<LevelColorData>( self );

			foreach( var color in auth.colors ) {
				colorsBuffer.Add( new LevelColorData { color = (Vector4)color.color, backgroundColor = (Vector4)color.backgroundColor, intensity = color.intensity } );
			}
		}
	}
}

[Serializable]
public class ColorWithIntensity {
	public Color color;
	public Color backgroundColor;
	public float intensity;
}

public struct LevelColorData : IBufferElementData {
	public float4 color;
	public float4 backgroundColor;
	public float intensity;
}

public struct LevelColorIndex : IComponentData {
	public int value;
}