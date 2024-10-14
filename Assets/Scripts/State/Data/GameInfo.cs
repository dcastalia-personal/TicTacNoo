using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GameInfo : MonoBehaviour {
	public Color neutralColor;
	public Color neutralBackground;

	public class Baker : Baker<GameInfo> {

		public override void Bake( GameInfo auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new GameColorData { neutral = (Vector4)auth.neutralColor, neutralBackground = (Vector4)auth.neutralBackground} );
		}
	}
}

public struct GameColorData : IComponentData {
	public float4 neutral;
	public float4 neutralBackground;
}