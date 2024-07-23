using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.SystemAPI;

public partial struct InitStatsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StatsData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var stats = GetSingletonRW<StatsData>();

		stats.ValueRW.highestLevelAchieved = PlayerPrefs.GetInt( "Highest Level", 0 );
		stats.ValueRW.averageSecondsToCompleteLevel = PlayerPrefs.GetFloat( "Average Seconds", 0f );
	}
}

[UpdateBefore(typeof(SaveStatsSys))]
public partial struct UpdateCurrentLevel : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, SwitchLevel>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var gameState = GetSingleton<GameStateData>();
		var stats = GetSingletonRW<StatsData>();

		if( gameState.nextLevel > stats.ValueRW.highestLevelAchieved ) {
			stats.ValueRW.highestLevelAchieved = gameState.nextLevel - 2; // don't count the first two levels
			
			if( TryGetSingleton( out LevelTimerData timer ) ) {
				stats.ValueRW.totalSecondsOnRun += timer.time;
				stats.ValueRW.averageSecondsToCompleteLevel = stats.ValueRW.totalSecondsOnRun / stats.ValueRW.highestLevelAchieved;
			}
		}
	}
}

[UpdateBefore(typeof(SwitchLevelSys))]
public partial struct SaveStatsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<GameStateData, SwitchLevel>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var stats = GetSingleton<StatsData>();

		PlayerPrefs.SetInt( "Highest Level", stats.highestLevelAchieved );
		PlayerPrefs.SetFloat( "Average Seconds", stats.averageSecondsToCompleteLevel );
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
		var levelTimer = GetSingletonRW<LevelTimerData>();

		levelTimer.ValueRW.time += SystemAPI.Time.DeltaTime;
	}
}

public partial struct MenuStatsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MenuAnimationData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var menuAnimation = GetSingletonRW<MenuAnimationData>();

		if( !menuAnimation.ValueRO.displayedStats && menuAnimation.ValueRO.elapsedTime > menuAnimation.ValueRO.statsInDelay ) {
			var stats = GetSingleton<StatsData>();
			
			var menuUIObj = GameObject.Find( "Menu UI" );
			var menuUI = menuUIObj.GetComponent<UIDocument>();
			var statsLayout = menuUI.rootVisualElement.Q( "Stats" );

			var highestLevelLabel = menuUI.rootVisualElement.Q<Label>( "Highest_Level" );
			highestLevelLabel.text = stats.highestLevelAchieved.ToString();
			
			var averageTimeLabel = menuUI.rootVisualElement.Q<Label>( "Average_Time" );
			averageTimeLabel.text = ((int)(stats.averageSecondsToCompleteLevel * 100f) / 100f).ToString( CultureInfo.CurrentCulture );

			foreach( var statItem in statsLayout.Children() ) {
				statItem.AddToClassList( "in" );
			}
		}

		menuAnimation.ValueRW.elapsedTime += SystemAPI.Time.DeltaTime;
	}
}

[UpdateBefore(typeof(ExitMenuSys))]
public partial struct TeardownMenuStatsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ExitMenuData>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var menuUIObj = GameObject.Find( "Menu UI" );
		var menuUI = menuUIObj.GetComponent<UIDocument>();
		var statsLayout = menuUI.rootVisualElement.Q( "Stats" );
		
		foreach( var statItem in statsLayout.Children() ) {
			statItem.RemoveFromClassList( "in" );
		}
	}
}