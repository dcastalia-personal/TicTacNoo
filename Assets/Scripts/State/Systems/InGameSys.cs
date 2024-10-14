using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(InitUISys))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitInGameUISys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<InGameData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var ui = GetSingleton<InGameUIData>();
		var matchInfoDisplay = ui.document.Value.rootVisualElement.Q<VisualElement>( "Match_Info" );
		var criticalMassDisplay = matchInfoDisplay.Q<Label>( "Critical_Mass" );
		criticalMassDisplay.text = GetSingleton<MatchInfoData>().criticalMass.ToString();
		matchInfoDisplay?.RemoveFromClassList( "out" );
	}
}

// [UpdateAfter(typeof(DestroySys))]
// public partial struct TeardownInGameUISys : ISystem {
// 	EntityQuery query;
//
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<InGameData, ShouldDestroy>() );
// 		state.RequireForUpdate<InGameData>();
// 	}
//
// 	public void OnUpdate( ref SystemState state ) {
// 		if( query.IsEmpty ) return;
// 		
// 		var ui = GetSingleton<InGameUIData>();
// 		var matchInfoDisplay = ui.document.Value.rootVisualElement.Q<VisualElement>( "Match_Info" );
// 		matchInfoDisplay.AddToClassList( "out" );
// 	}
// }