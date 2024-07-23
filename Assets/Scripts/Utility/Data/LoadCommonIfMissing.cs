using Unity.Entities;
using UnityEngine;

public class LoadCommonIfMissing : MonoBehaviour {

	public class Baker : Baker<LoadCommonIfMissing> {

		public override void Bake( LoadCommonIfMissing auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LoadCommonIfMissingData {} );
		}
	}
}

public struct LoadCommonIfMissingData : IComponentData {}