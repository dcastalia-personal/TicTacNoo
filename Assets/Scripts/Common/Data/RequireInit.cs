using Unity.Entities;
using UnityEngine;

public class RequireInit : MonoBehaviour {

	public class Baker : Baker<RequireInit> {

		public override void Bake( RequireInit auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RequireInitData {} );
		}
	}
}

public struct RequireInitData : IComponentData {}