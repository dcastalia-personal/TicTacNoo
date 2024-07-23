using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class FailureAck : MonoBehaviour {
	public VisualTreeAsset failureUIPrefab;

	public class Baker : Baker<FailureAck> {

		public override void Bake( FailureAck auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FailureAckData { failureUIPrefab = new UnityObjectRef<VisualTreeAsset> { Value = auth.failureUIPrefab } } );
		}
	}
}

public struct FailureAckData : IComponentData {
	public UnityObjectRef<VisualTreeAsset> failureUIPrefab;
}