using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))] [UpdateAfter(typeof(InitAssociatedGOSys))]
public partial struct InitUISys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<InGameUIData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var uiData in Query<RefRW<InGameUIData>>() ) {
			var document = GameObject.Find( "In-Game UI" ).GetComponent<UIDocument>();
			uiData.ValueRW.document.Value = document;

			var em = state.EntityManager;
			var exitButton = document.rootVisualElement.Q<Button>( "Exit" );

			var gameStateEntity = GetSingletonEntity<GameStateData>();
			exitButton.clicked += () => {
				em.AddComponentData( gameStateEntity, new PauseGameTag() );
			};
		}
	}
}