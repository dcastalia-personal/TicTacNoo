using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class LaserPointer : MonoBehaviour {
	public GameObject beamImpactPrefab;
	public GameObject audioIn;
	public GameObject audioLoop;
	public GameObject audioOut;

	public class Baker : Baker<LaserPointer> {

		public override void Bake( LaserPointer auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LaserPointerData {
				beamImpactPrefab = GetEntity( auth.beamImpactPrefab, TransformUsageFlags.Dynamic ),
				audioInPrefab = GetEntity( auth.audioIn, TransformUsageFlags.Dynamic ),
				audioOutPrefab = GetEntity( auth.audioOut, TransformUsageFlags.Dynamic ),
				audioLoopPrefab = GetEntity( auth.audioLoop, TransformUsageFlags.Dynamic ),
			} );
			AddComponent( self, new LaserLength {} );
			AddComponent( self, new CurrentColorData {} );
			AddBuffer<LinkedEntityGroup>( self );
		}
	}
}

public struct LaserPointerData : IComponentData {
	public Entity beamImpactPrefab;
	public Entity target;
	
	public Entity audioInPrefab;
	public Entity audioLoopPrefab;
	public Entity audioOutPrefab;

	public Entity audioInInstance;
	public Entity audioLoopInstance;
	public Entity audioOutInstance;
}

[MaterialProperty("_Target_Distance")] public struct LaserLength : IComponentData { public float value; }