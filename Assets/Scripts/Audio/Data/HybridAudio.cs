using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using UnityEngine.Audio;

public class HybridAudio : MonoBehaviour {
	public GameObject template;
	public List<AudioMixerGroup> mixerGroups;

	public class Baker : Baker<HybridAudio> {

		public override void Bake( HybridAudio auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new HybridAudioData { template = new() { Value = auth.template } } );
			
			AddComponentObject( self, new HybridAudioPool { pool = new(), groups = auth.mixerGroups } );
		}
	}
}

// for some reason associating an AudioSource with an entity crashes when the EntityScene housing it is loaded in a build; using the AudioData singleton to refer to it for now
public struct HybridAudioData : IComponentData {
	public UnityObjectRef<GameObject> template;
}

public class HybridAudioPool : IComponentData {
	// public AudioSource template;
	public List<AudioSource> pool;
	public List<AudioMixerGroup> groups;
}