using Unity.Entities;
using UnityEngine;

public class ShareColorOnContact : MonoBehaviour {

	public class Baker : Baker<ShareColorOnContact> {

		public override void Bake( ShareColorOnContact auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ShareColorOnContactData {} );
		}
	}
}

public struct ShareColorOnContactData : IComponentData {}