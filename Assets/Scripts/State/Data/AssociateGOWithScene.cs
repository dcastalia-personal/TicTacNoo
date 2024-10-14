using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(RequireInit))]
public class AssociateGOWithScene : MonoBehaviour {
	public GameObject prefab;

	public class Baker : Baker<AssociateGOWithScene> {

		public override void Bake( AssociateGOWithScene auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new AssociateGOWithSceneData { prefab = new UnityObjectRef<GameObject> { Value = auth.prefab } } );
		}
	}
}

public struct AssociateGOWithSceneData : IComponentData {
	public UnityObjectRef<GameObject> prefab;
	public UnityObjectRef<GameObject> instance;
}