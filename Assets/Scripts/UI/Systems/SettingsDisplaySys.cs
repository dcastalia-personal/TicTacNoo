using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup)), UpdateAfter(typeof(InitAssociatedGOSys)), UpdateAfter(typeof(InitLevelAudioSys)), UpdateAfter(typeof(InitHybridAudioSys))]
public partial struct InitDisplaySettingsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SettingsDisplayData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (settingsDisplayData, self) in Query<RefRW<SettingsDisplayData>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			var em = state.EntityManager;
			
			var settingsUIProxy = GameObject.Find( "Settings UI" );
			var settingsDoc = settingsUIProxy.GetComponent<UIDocument>();
			settingsDisplayData.ValueRW.document = new UnityObjectRef<UIDocument> { Value = settingsDoc };

			var gameStateEntity = GetSingletonEntity<GameStateData>();
			var gameState = GetComponent<GameStateData>( gameStateEntity );

			var parentContainer = settingsDoc.rootVisualElement.Q<VisualElement>( "Parent" );
			parentContainer.SwitchStyleOnFirstFrame( "out", "in" );

			var audio = GetSingletonEntity<HybridAudioData>();
			var audioManaged = em.GetComponentObject<HybridAudioPool>( audio );
			
			var musicSlider = settingsDoc.rootVisualElement.Q<Slider>( "Music_Slider" );
			musicSlider.value = PlayerPrefs.GetFloat( "Music", 1f );

			var effectsSlider = settingsDoc.rootVisualElement.Q<Slider>( "Effects_Slider" );
			effectsSlider.value = PlayerPrefs.GetFloat( "Effects", 1f );
			
			var sensitivitySlider = settingsDoc.rootVisualElement.Q<Slider>( "Sensitivity_Slider" );
			sensitivitySlider.value = PlayerPrefs.GetFloat( "Sensitivity", 1f );

			var backButton = settingsDoc.rootVisualElement.Q<Button>( "Back" );

			musicSlider.RegisterValueChangedCallback( value => {
				var db = math.max( math.log10( value.newValue ) * 30f, -80f );
				audioManaged.groups[ 0 ].audioMixer.SetFloat( "Music", db );
				PlayerPrefs.SetFloat( "Music", value.newValue );
			} );
			
			effectsSlider.RegisterValueChangedCallback( value => {
				var db = math.max( math.log10( value.newValue ) * 30f, -80f );
				audioManaged.groups[ 0 ].audioMixer.SetFloat( "Effects", db );
				PlayerPrefs.SetFloat( "Effects", value.newValue );
			} );
			
			sensitivitySlider.RegisterValueChangedCallback( value => {
				PlayerPrefs.SetFloat( "Sensitivity", value.newValue );
			} );
			
			backButton.clicked += () => {
				gameState.nextLevel = 0;
				em.SetComponentData( gameStateEntity, gameState );
				
				parentContainer.RemoveFromClassList( "in" );
				parentContainer.AddToClassList( "out" );
				parentContainer.RegisterCallbackOnce<TransitionEndEvent>( evt => {
					em.AddComponent<SwitchLevel>( gameStateEntity );
				} );

				var settingsDisplayQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SettingsDisplayData>() );
				var statsDisplayDataOnDemand = settingsDisplayQuery.GetSingleton<SettingsDisplayData>();
				em.SetComponentEnabled<FadeOut>( statsDisplayDataOnDemand.musicInstance, true );

				PlayerPrefs.Save();
			};

			var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			var settingsDisplayDataCopy = settingsDisplayData.ValueRO;
			settingsDisplayDataCopy.musicInstance = ecb.Instantiate( settingsDisplayData.ValueRO.music );
			ecb.SetComponent( self, settingsDisplayDataCopy );
		}
	}
}