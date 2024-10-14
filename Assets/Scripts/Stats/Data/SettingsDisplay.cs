using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(RequireInit))]
public class SettingsDisplay : MonoBehaviour {
    public GameObject music;

    public class Baker : Baker<SettingsDisplay> {

        public override void Bake( SettingsDisplay auth ) {
            var self = GetEntity( TransformUsageFlags.None );
            AddComponent( self, new SettingsDisplayData { music = GetEntity( auth.music, TransformUsageFlags.Dynamic ) } );
        }
    }
}

public struct SettingsDisplayData : IComponentData {
    public UnityObjectRef<UIDocument> document;
    public Entity music;
    public Entity musicInstance;
}