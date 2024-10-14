using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(RequireInit))]
public class AudioDefinition : MonoBehaviour {
	public List<AudioClip> clips;
	public float volume;
	public bool loop;
	public float mix;

	public int mixerGroup;

	public float fadeInDuration;
	public SharedCurve fadeInCurve;
	
	public float fadeOutDuration;
	public SharedCurve fadeOutCurve;
	
	public class Baker : Baker<AudioDefinition> {
		
		public override void Bake( AudioDefinition auth ) {
			var fadeInBlobRef = CurveBlob.CreateCurveBlob( auth.fadeInCurve );
			AddBlobAsset( ref fadeInBlobRef, out _ );
			
			var fadeOutBlobRef = CurveBlob.CreateCurveBlob( auth.fadeOutCurve );
			AddBlobAsset( ref fadeOutBlobRef, out _ );
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new AudioDefinitionData { volume = auth.volume, loop = auth.loop, mix = auth.mix, lastClipIndexPlayed = -1, group = auth.mixerGroup } );
			AddComponent( self, new FadeIn { fadeCurve = fadeInBlobRef, duration = auth.fadeInDuration } );
			AddComponent( self, new FadeOut { fadeCurve = fadeOutBlobRef, duration = auth.fadeOutDuration } ); SetComponentEnabled<FadeOut>( self, false );

			var audioRefs = AddBuffer<AudioRef>( self );
			foreach( var clip in auth.clips ) {
				audioRefs.Add( new AudioRef { clip = new UnityObjectRef<AudioClip>() { Value = clip } } );
			}
		}
	}
}

public struct AudioDefinitionData : IComponentData {
	public float volume;
	public bool loop;
	public float time; // doesn't loop
	public float mix;
	public int group;
	
	public UnityObjectRef<AudioSource> source;
	public float duration;

	public int lastClipIndexPlayed;
}

[InternalBufferCapacity( 1 )]
public struct AudioRef : IBufferElementData {
	public UnityObjectRef<AudioClip> clip;
}

public struct FadeIn : IComponentData, IEnableableComponent {
	public float duration;
	public BlobAssetReference<CurveBlob> fadeCurve;
}

public struct FadeOut : IComponentData, IEnableableComponent {
	public float duration;
	public float time;
	public float startVolume;
	public BlobAssetReference<CurveBlob> fadeCurve;
}