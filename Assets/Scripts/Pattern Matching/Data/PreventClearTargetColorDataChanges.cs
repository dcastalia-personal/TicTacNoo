using Unity.Entities;
using UnityEngine;

public class PreventClearTargetColorDataChanges : MonoBehaviour {

	public class Baker : Baker<PreventClearTargetColorDataChanges> {

		public override void Bake( PreventClearTargetColorDataChanges auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new PreventClearColorChanges {} );
		}
	}
}

public struct PreventClearColorChanges : IComponentData {}