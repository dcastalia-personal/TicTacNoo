using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial struct ClearInitRequirementSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		state.EntityManager.RemoveComponent<RequireInitData>( query );
	}
}