using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class FailureAck : MonoBehaviour {
	public VisualTreeAsset failureUIPrefab;
	public GameObject failureStabPrefab;
	public GameObject failureLoopPrefab;

	public class Baker : Baker<FailureAck> {

		public override void Bake( FailureAck auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new FailureAckData {
				failureUIPrefab = new UnityObjectRef<VisualTreeAsset> { Value = auth.failureUIPrefab }, 
				failureLoopPrefab = GetEntity( auth.failureLoopPrefab, TransformUsageFlags.Dynamic ),
				failureStabPrefab = GetEntity( auth.failureStabPrefab, TransformUsageFlags.Dynamic ),
			} );
		}
	}
}

public struct FailureAckData : IComponentData {
	public UnityObjectRef<VisualTreeAsset> failureUIPrefab;
	public Entity failureStabPrefab;
	public Entity failureLoopPrefab;
	public Entity failureLoopInstance;
}