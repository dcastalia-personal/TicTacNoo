using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class UI : MonoBehaviour {

	public class Baker : Baker<UI> {

		public override void Bake( UI auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new UIData {} );
		}
	}
}

public struct UIData : IComponentData {
	public UnityObjectRef<UIDocument> document;
}