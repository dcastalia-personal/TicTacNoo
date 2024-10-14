using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MatchTargetPos : MonoBehaviour {
    public GameObject target;
    public Vector3 localOffset;

    public class Baker : Baker<MatchTargetPos> {

        public override void Bake( MatchTargetPos auth ) {
            var self = GetEntity( TransformUsageFlags.None );
            AddComponent( self, new MatchTargetPosData { target = GetEntity( auth.target, TransformUsageFlags.Dynamic ), localOffset = auth.localOffset } );
        }
    }
}

public struct MatchTargetPosData : IComponentData {
    public Entity target;
    public float3 localOffset;
}