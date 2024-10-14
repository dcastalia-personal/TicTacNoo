using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameUI : MonoBehaviour {

	public class Baker : Baker<InGameUI> {

		public override void Bake( InGameUI auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new InGameUIData {} );
		}
	}
}

public struct InGameUIData : IComponentData {
	public UnityObjectRef<UIDocument> document;
}