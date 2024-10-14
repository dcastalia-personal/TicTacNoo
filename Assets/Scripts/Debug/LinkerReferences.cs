using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

// create a hard ref to game objects that are necessary to include in a build if otherwise not caught by the linker
public class LinkerReferences : MonoBehaviour {
	public VisualTreeAsset[] VisualTreeAssets;
	public AudioMixer mixer;
	public AudioMixerSnapshot MixerSnapshot;
}
