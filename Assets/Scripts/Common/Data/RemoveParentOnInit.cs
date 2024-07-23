using Unity.Entities;
using UnityEngine;

public class RemoveParentOnInit : MonoBehaviour {

	public class Baker : Baker<RemoveParentOnInit> {

		public override void Bake( RemoveParentOnInit auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RemoveParentData {} );
		}
	}
}

public struct RemoveParentData : IComponentData {
	
}