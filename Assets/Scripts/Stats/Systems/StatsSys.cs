using System.Globalization;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

public partial struct LoadStatsData : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatData, LoadTag>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		
		foreach( var (_, self) in Query<LoadTag>().WithAll<StatData>().WithEntityAccess() ) {
			var json = PlayerPrefs.GetString( "Save", "" );
      var loaded = JsonUtility.FromJson<StatsData>( json );
      if( loaded == null ) continue;

      var stats = GetBuffer<StatData>( self );

      for( int index = 0; index < stats.Length; index++ ) {
	      if( loaded.elements.Length > index ) stats[ index ] = loaded.elements[ index ];
      }
		}

		ecb.RemoveComponent<LoadTag>( query, EntityQueryCaptureMode.AtPlayback );
	}
}

[UpdateBefore(typeof(SwitchLevelSys))]
public partial struct SaveStatsSys : ISystem {
	EntityQuery query;
	EntityQuery victoryEnabledQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, SwitchLevel>() );
		state.RequireForUpdate( query );
		
		victoryEnabledQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<VictoryEnabled>() );
	}

	public void OnUpdate( ref SystemState state ) {
		var statsBuf = GetSingletonBuffer<StatData>();
		var gameState = GetSingleton<GameStateData>();

		// assign current lowest time
		if( TryGetSingletonRW( out RefRW<LevelTimerData> timer ) ) {
			var currentStats = statsBuf[ gameState.curLevelIndex ];

			var secondsToRecord = timer.ValueRO.value;
			if( victoryEnabledQuery.IsEmpty ) secondsToRecord = float.MaxValue;

			if( currentStats.lowestTime == 0f || secondsToRecord < currentStats.lowestTime ) {
				statsBuf[ gameState.curLevelIndex ] = new StatData { lowestTime = secondsToRecord };
			}
			
			// Debug.Log( $"Time for level {gameState.curLevelIndex} is {timer.ValueRO.value}" );

			timer.ValueRW.value = 0f;
		}
		
		var saved = new StatsData { elements = statsBuf.ToNativeArray( Allocator.Temp ).ToArray() };
		var json = JsonUtility.ToJson( saved );

		PlayerPrefs.SetString( "Save", json );
	}
}

public partial struct CountTimeThisLevel : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<InGameData>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<LevelTimerData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var levelTimerData in Query<RefRW<LevelTimerData>>() ) {
			levelTimerData.ValueRW.value += SystemAPI.Time.DeltaTime;
		}
	}
}

// Display

[UpdateInGroup(typeof(InitializationSystemGroup)), UpdateAfter(typeof(InitAssociatedGOSys)), UpdateAfter(typeof(InitLevelAudioSys))]
public partial struct InitDisplayStatsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatsDisplayData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (statsDisplayData, self) in Query<RefRW<StatsDisplayData>>().WithAll<RequireInitData>().WithEntityAccess() ) {
			var statsBufEntity = GetSingletonEntity<StatData>();
			var statsBuf = GetSingletonBuffer<StatData>();
			var em = state.EntityManager;
			
			var statsUIProxy = GameObject.Find( "Stats UI" );
			var statsDoc = statsUIProxy.GetComponent<UIDocument>();
			statsDisplayData.ValueRW.document = new UnityObjectRef<UIDocument> { Value = statsDoc };

			var parentContainer = statsDoc.rootVisualElement.Q<VisualElement>( "Parent" );
			parentContainer.SwitchStyleOnFirstFrame( "out", "in" );

			var listContainer = statsDoc.rootVisualElement.Q<ScrollView>( "Levels_List" );

			var gameStateEntity = GetSingletonEntity<GameStateData>();
			var gameState = GetComponent<GameStateData>( gameStateEntity );
			var levelsBuf = GetBuffer<Level>( gameStateEntity );

			int displayedIndex = 1;
			for( int sceneIndex = 0; sceneIndex < statsBuf.Length; sceneIndex++ ) {
				var timeTaken = statsBuf[ sceneIndex ].lowestTime;
				if( timeTaken == 0f ) continue;
				
				var entry = statsDisplayData.ValueRO.statsEntryPrefab.Value.Instantiate();
				
				var number = entry.Q<Label>( "Number_Label" );
				number.text = $"{displayedIndex.ToString()}. {levelsBuf[sceneIndex].name}";
				displayedIndex++;

				var timeLabel = entry.Q<Label>( "Time_Label" );
				var timeTakenText = $"{((int)timeTaken).ToString( CultureInfo.InvariantCulture )}s";
				if( timeTakenText == "-1s" ) timeTakenText = "-";
				timeLabel.text = timeTakenText;

				var bestTimeThisLevel = levelsBuf[ sceneIndex ].bestTime;
				for( int timingIndex = 1; timingIndex < GameStateData.maxNumStars; timingIndex++ ) {
					var starIndex = GameStateData.maxNumStars - timingIndex;
					var timeForThisStar = bestTimeThisLevel * timingIndex;
					
					entry.Q<VisualElement>( $"Star{starIndex}" ).visible = timeTaken <= timeForThisStar;
				}

				var replayButton = entry.Q<Button>( "Load_Button" );
				int levelToLoad = sceneIndex;
				replayButton.clicked += () => {
					gameState.nextLevel = levelToLoad;
					em.SetComponentData( gameStateEntity, gameState );

					parentContainer.RemoveFromClassList( "in" );
					parentContainer.AddToClassList( "out" );
					parentContainer.RegisterCallbackOnce<TransitionEndEvent>( evt => {
						em.AddComponent<SwitchLevel>( gameStateEntity );
					} );
					
					var statsDisplayQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatsDisplayData>() );
					var statsDisplayDataOnDemand = statsDisplayQuery.GetSingleton<StatsDisplayData>();
					em.SetComponentEnabled<FadeOut>( statsDisplayDataOnDemand.musicInstance, true );
				};

				listContainer.contentContainer.Add( entry );
			}

			var resetButton = statsDoc.rootVisualElement.Q<Button>( "Reset_Button" );
			var exitButton = statsDoc.rootVisualElement.Q<Button>( "Exit_Button" );

			resetButton.clicked += () => {
				var curStatsBuf = em.GetBuffer<StatData>( statsBufEntity );
				for( int index = 0; index < curStatsBuf.Length; index++ ) {
					curStatsBuf[ index ] = new StatData {};
				}

				listContainer.contentContainer.Clear();

				Object.Destroy( statsDoc.gameObject );
				em.AddComponent<RequireInitData>( self );
				
				var statsDisplayQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatsDisplayData>() );
				var statsDisplayDataOnDemand = statsDisplayQuery.GetSingleton<StatsDisplayData>();
				em.SetComponentEnabled<FadeOut>( statsDisplayDataOnDemand.musicInstance, true );
			};

			exitButton.clicked += () => {
				gameState.nextLevel = 0;
				em.SetComponentData( gameStateEntity, gameState );
				
				parentContainer.RemoveFromClassList( "in" );
				parentContainer.AddToClassList( "out" );
				parentContainer.RegisterCallbackOnce<TransitionEndEvent>( evt => {
					em.AddComponent<SwitchLevel>( gameStateEntity );
				} );

				var statsDisplayQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatsDisplayData>() );
				var statsDisplayDataOnDemand = statsDisplayQuery.GetSingleton<StatsDisplayData>();
				em.SetComponentEnabled<FadeOut>( statsDisplayDataOnDemand.musicInstance, true );
			};

			var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			var statsDisplayDataCopy = statsDisplayData.ValueRO;
			statsDisplayDataCopy.musicInstance = ecb.Instantiate( statsDisplayData.ValueRO.music );
			ecb.SetComponent( self, statsDisplayDataCopy );
		}
	}
}