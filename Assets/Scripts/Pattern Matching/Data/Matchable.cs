using Unity.Entities;
using UnityEngine;

public class Matchable : MonoBehaviour {
	public bool debug;

	public class Baker : Baker<Matchable> {

		public override void Bake( Matchable auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchableData { debug = auth.debug } );
			AddBuffer<MatchCandidate>( self );
			AddComponent<Matched>( self ); SetComponentEnabled<Matched>( self, false );
			// AddComponent<MatchCandidatesChanged>( self ); SetComponentEnabled<MatchCandidatesChanged>( self, false );
		}
	}
}

public struct MatchableData : IComponentData {
	public bool debug;
}

[InternalBufferCapacity(3)] // usually, we are matching three; it'd be nice if this could be set dynamically, though, since the demands change per level
public struct MatchCandidate : IBufferElementData {
	public Entity value;
}

public struct MatchCandidatesChanged : IComponentData, IEnableableComponent {}

public struct Matched : IComponentData, IEnableableComponent {}