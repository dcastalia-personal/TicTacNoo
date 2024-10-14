using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct MatchTargetSys : ISystem {
	EntityQuery query;
	ComponentLookup<LocalToWorld> localToWorldLookup;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		localToWorldLookup = state.GetComponentLookup<LocalToWorld>( isReadOnly: false );
        
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchTargetPosData, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		localToWorldLookup.Update( ref state );
		new MatchTargetJob { localToWorldLookup = localToWorldLookup }.ScheduleParallel();
	}

	[BurstCompile] partial struct MatchTargetJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<LocalToWorld> localToWorldLookup;
		
		void Execute( Entity self, in MatchTargetPosData matchTargetPositionData, ref LocalTransform transform ) {
			var myLtw = localToWorldLookup[ self ];
			var targetLtw = localToWorldLookup[ matchTargetPositionData.target ];
			var offsetPosition = math.transform( targetLtw.Value, matchTargetPositionData.localOffset );

			var trs = myLtw.Value;
			var pos = trs.c3;
			pos = new float4( offsetPosition, 0f );
			trs.c3 = pos;
			myLtw.Value = trs;

			localToWorldLookup[ self ] = myLtw;
			transform.Position = offsetPosition;
		}
	}
}