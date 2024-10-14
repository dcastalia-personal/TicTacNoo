using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))] [UpdateAfter(typeof(InitAssociatedGOSys))]
public partial struct InitStorySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StoryAnimationData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var storyUI = GameObject.Find( "Story UI" ).GetComponent<UIDocument>();
		
		var storyAnim = GetSingletonRW<StoryAnimationData>();
		storyAnim.ValueRW.storyUI.Value = storyUI;
		storyAnim.ValueRW.stages = storyUI.rootVisualElement.Q<VisualElement>( "Stories" ).childCount;

		storyUI.rootVisualElement.Q<VisualElement>( "Story_0" ).SwitchStyleOnFirstFrame( "", "in" );
		storyAnim.ValueRW.curStage = 1;

		storyAnim.ValueRW.musicInstance = state.EntityManager.Instantiate( storyAnim.ValueRO.musicPrefab );
	}
}

[UpdateAfter(typeof(CollectInput))]
public partial struct AdvanceStorySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PointerReleased>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<StoryAnimationData>();
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var storyAnim = GetSingletonRW<StoryAnimationData>();

		if( storyAnim.ValueRO.curStage < storyAnim.ValueRO.stages ) {
			var curLabel = storyAnim.ValueRO.storyUI.Value.rootVisualElement.Q<Label>( $"Story_{storyAnim.ValueRO.curStage}" );
			curLabel.AddToClassList( "in" );

			storyAnim.ValueRW.curStage++;
		}
		else {
			for( int index = 0; index < storyAnim.ValueRO.stages; index++ ) {
				var label = storyAnim.ValueRO.storyUI.Value.rootVisualElement.Q<Label>( $"Story_{index}" );
				label.RemoveFromClassList( "in" );
			}
			
			var gameStateEntity = GetSingletonEntity<GameStateData>();
			var gameState = GetSingletonRW<GameStateData>();
			gameState.ValueRW.nextLevel = storyAnim.ValueRO.nextLevel;

			var em = state.EntityManager;
			em.SetComponentEnabled<FadeOut>( storyAnim.ValueRO.musicInstance, true );
			
			var firstLabel = storyAnim.ValueRO.storyUI.Value.rootVisualElement.Q<Label>( "Story_0" );
			var lastLabel = storyAnim.ValueRO.storyUI.Value.rootVisualElement.Q<Label>( $"Story_{storyAnim.ValueRO.stages - 1}" );

			var labelFurthestFromOut = lastLabel;
			if( firstLabel.resolvedStyle.opacity > lastLabel.resolvedStyle.opacity ) labelFurthestFromOut = firstLabel;
			labelFurthestFromOut.RegisterCallbackOnce<TransitionEndEvent>( evt => {
				em.AddComponent<SwitchLevel>( gameStateEntity );
			} );

			var storyAnimEntity = GetSingletonEntity<StoryAnimationData>();
			em.DestroyEntity( storyAnimEntity );
		}
	}
}