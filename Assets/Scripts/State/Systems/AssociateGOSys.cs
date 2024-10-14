using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitAssociatedGOSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AssociateGOWithSceneData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (associateGOWithSceneData, self) in Query<RefRW<AssociateGOWithSceneData>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			var prefab = associateGOWithSceneData.ValueRO.prefab.Value;
			var instance = Object.Instantiate( prefab );
			associateGOWithSceneData.ValueRW.instance = new UnityObjectRef<GameObject> { Value = instance };
			instance.name = prefab.name;
		}
	}
}

[UpdateBefore(typeof(SwitchLevelSys))]
public partial struct CleanupAssociatedGOSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<AssociateGOWithSceneData>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<SwitchLevel>();
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var associatedGo in Query<RefRO<AssociateGOWithSceneData>>() ) {
			Object.Destroy( associatedGo.ValueRO.instance.Value );
		}
	}
}