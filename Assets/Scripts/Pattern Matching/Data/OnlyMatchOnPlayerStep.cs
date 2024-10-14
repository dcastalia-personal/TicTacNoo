using Unity.Entities;
using UnityEngine;

public class OnlyMatchOnPlayerStep : MonoBehaviour {

	public class Baker : Baker<OnlyMatchOnPlayerStep> {

		public override void Bake( OnlyMatchOnPlayerStep auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new OnlyMatchOnPlayerStepData {} );
		}
	}
}

public struct OnlyMatchOnPlayerStepData : IComponentData {}