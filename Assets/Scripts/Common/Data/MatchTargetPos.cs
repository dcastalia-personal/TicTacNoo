using Unity.Entities;
using UnityEngine;

public class MatchTargetPos : MonoBehaviour {
    public GameObject target;

    public class Baker : Baker<MatchTargetPos> {

        public override void Bake( MatchTargetPos auth ) {
            var self = GetEntity( TransformUsageFlags.None );
            AddComponent( self, new MatchTargetPosData { target = GetEntity( auth.target, TransformUsageFlags.Dynamic ) } );
        }
    }
}

public struct MatchTargetPosData : IComponentData {
    public Entity target;
    public float startScale;
}