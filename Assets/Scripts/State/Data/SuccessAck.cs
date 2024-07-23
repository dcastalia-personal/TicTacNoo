using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class SuccessAck : MonoBehaviour {
	public VisualTreeAsset successUIPrefab;

	public class Baker : Baker<SuccessAck> {

		public override void Bake( SuccessAck auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SuccessAckData { successUIPrefab = new UnityObjectRef<VisualTreeAsset> { Value = auth.successUIPrefab } } );
		}
	}
}

public struct SuccessAckData : IComponentData {
	public UnityObjectRef<VisualTreeAsset> successUIPrefab;
}