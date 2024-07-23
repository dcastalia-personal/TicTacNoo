using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

public partial struct MatchTargetSys : ISystem {
	EntityQuery query;
	ComponentLookup<LocalToWorld> localToWorldLookup;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		localToWorldLookup = state.GetComponentLookup<LocalToWorld>( isReadOnly: true );
        
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchTargetPosData, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		localToWorldLookup.Update( ref state );
		new MatchTargetJob { localToWorldLookup = localToWorldLookup }.ScheduleParallel();
	}

	[BurstCompile] partial struct MatchTargetJob : IJobEntity {
		[ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookup;
		
		void Execute( in MatchTargetPosData matchTargetPositionData, ref LocalTransform transform ) {
			var targetLtw = localToWorldLookup[ matchTargetPositionData.target ];
			transform.Position = targetLtw.Position;
		}
	}
}