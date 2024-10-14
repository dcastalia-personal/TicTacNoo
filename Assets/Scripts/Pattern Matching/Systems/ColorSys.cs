using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial struct InitMatchableColorSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableColor, RequireInitData, TargetColorData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitMatchableColorJob { neutralColor = GetSingleton<GameColorData>().neutral }.ScheduleParallel( query );
	}

	[BurstCompile] partial struct InitMatchableColorJob : IJobEntity {
		public float4 neutralColor;
		void Execute( Entity self, ref MatchableColor matchableColor, ref TargetColorData targetColorData ) {
			matchableColor.value = neutralColor;
			targetColorData.baseColor = neutralColor;
		}
	}
}

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
[UpdateAfter(typeof(InitMatchableColorSys))]
public partial struct InitCurColor : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<TargetColorData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (targetColorData, curColorData, curEmissionData) in Query<RefRO<TargetColorData>, RefRW<CurrentColorData>, RefRW<CurrentEmissionData>>().WithAll<RequireInitData>() ) {
			curColorData.ValueRW.value = targetColorData.ValueRO.baseColor;
			curEmissionData.ValueRW.value = targetColorData.ValueRO.emission;
		}
	}
}

[UpdateBefore(typeof(EnableTargetColorOnPressed))]
public partial struct ClearMatchableColorChangedSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableColorChanged>().WithNone<PreventClearColorChanges>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		state.EntityManager.SetComponentEnabled<MatchableColorChanged>( query, false );
	}
}

[UpdateBefore(typeof(SetMatchableColorByIndexSys))] [UpdateAfter(typeof(ClearPlayerStepSys))]
public partial struct EnableTargetColorOnPressed : ISystem {
	EntityQuery stepQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
		
		state.RequireForUpdate<InGameData>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( !stepQuery.IsEmpty ) return;

		// I wish EnabledRefRW gathered components that were disabled without forcing you to ignore the enabled state across the whole query
		// if you're expecting to write to the enabled state, half the time aren't you going to want to fetch components that aren't enabled?
		foreach( var (_, self) in Query<RefRO<Pressed>>().WithAll<TargetColorData, CycleColorsOnPressedData>().WithEntityAccess() ) {
			SetComponentEnabled<SetTargetColorByIndexData>( self, true );
		}
	}
}

public partial struct SetMatchableColorByIndexSys : ISystem {
	
	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (targetColorData, setByIndexEnabled, matchableColor, self) in Query<RefRO<TargetColorData>, EnabledRefRW<SetTargetColorByIndexData>, RefRW<MatchableColor>>().WithEntityAccess() ) {
			var colorDirectory = GetSingletonBuffer<LevelColorElem>();
			var colorIndex = GetSingletonRW<LevelColorIndex>();

			var curBaseColor = colorDirectory[ colorIndex.ValueRO.current ].color;
			if( targetColorData.ValueRO.baseColor.Equals( curBaseColor ) ) {
				setByIndexEnabled.ValueRW = false;
				return;
			}

			var oldColor = colorDirectory[ colorIndex.ValueRO.current ];
			var newColorIndex = colorIndex.ValueRW.current = (colorIndex.ValueRO.current + 1) % colorDirectory.Length;
			
			// change target color
			matchableColor.ValueRW.value = curBaseColor;
			matchableColor.ValueRW.emission = colorDirectory[ colorIndex.ValueRO.current ].intensity;
			colorIndex.ValueRW.current = newColorIndex;
			setByIndexEnabled.ValueRW = false;
			SetComponentEnabled<MatchableColorChanged>( self, true );

			// change camera background
			var cameraEntity = GetSingletonEntity<CameraData>();
			var cameraData = GetComponent<TargetColorData>( cameraEntity );
			cameraData.baseColor = colorDirectory[ newColorIndex ].backgroundColor;
			SetComponent( cameraEntity, cameraData );
			SetComponentEnabled<TargetColorData>( cameraEntity, true );
			
			// step the player
			var playerStepEntity = GetSingletonEntity<PlayerStepTag>();
			SetComponentEnabled<PlayerStepped>( playerStepEntity, true );

			var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

			ecb.Instantiate( oldColor.tone );
		}
	}
}

[UpdateBefore(typeof(ClearMatchableColorChangedSys))]
public partial struct MatchableToTargetColSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableColor, MatchableColorChanged, TargetColorData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new MatchableToTargetColJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct MatchableToTargetColJob : IJobEntity {
		void Execute( in MatchableColor matchableColor, ref TargetColorData targetColorData ) {
			targetColorData.baseColor = matchableColor.value;
			targetColorData.emission = matchableColor.emission;
		}
	}
}

public partial struct UpdateCurColorSys : ISystem {
	EntityQuery query;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<TargetColorData, CurrentColorData, CurrentEmissionData>() );
		state.RequireForUpdate( query );
	}
	
	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		new UpdateColorSysJob { deltaTime = deltaTime }.ScheduleParallel( query );
	}

	[BurstCompile] partial struct UpdateColorSysJob : IJobEntity {
		public float deltaTime;
		
		void Execute( in TargetColorData targetColorData, ref CurrentColorData currentColorData, ref CurrentEmissionData currentEmissionData ) {
			currentColorData.value = math.lerp( currentColorData.value, targetColorData.baseColor, targetColorData.tweenSpeed * deltaTime );
			currentEmissionData.value = math.lerp( currentEmissionData.value, targetColorData.emission, targetColorData.tweenSpeed * deltaTime );
		}
	}
}

[UpdateBefore(typeof(ClearMatchableColorChangedSys))]
public partial struct EnableFadeColorSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchableColorChanged>().WithPresent<FadeColorData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (matchableColor, self) in Query<RefRO<MatchableColor>>().WithAll<MatchableColorChanged>().WithPresent<FadeColorData>().WithEntityAccess() ) {
			if( matchableColor.ValueRO.value.Equals( GetSingleton<GameColorData>().neutral ) ) continue;
			SetComponentEnabled<FadeColorData>( self, true );
		}

		foreach( var (fadeColor, self) in Query<RefRW<FadeColorData>>().WithAll<MatchableColorChanged>().WithEntityAccess() ) {
			fadeColor.ValueRW.time = 0f;
		}
	}
}

public partial struct FadeColorSys : ISystem {
	EntityQuery query;
	EntityQuery stepQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<FadeColorData, TargetColorData, MatchableColor>().WithPresent<MatchableColorChanged>() );
		state.RequireForUpdate( query );
		
		stepQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PlayerStepData>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		if( stepQuery.IsEmpty ) return;
		
		new FadeColorJob { deltaTime = SystemAPI.Time.DeltaTime, neutralColor = GetSingleton<GameColorData>().neutral }.ScheduleParallel( query );
	}

	[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	[BurstCompile] partial struct FadeColorJob : IJobEntity {
		public float deltaTime;
		public float4 neutralColor;
		
		void Execute( ref FadeColorData fadeColorData, EnabledRefRW<FadeColorData> fadeColorEnabled, ref TargetColorData targetColorData, ref MatchableColor matchableColor, EnabledRefRW<MatchableColorChanged> matchableColorChanged ) {
			fadeColorData.time = math.min( fadeColorData.time + deltaTime * fadeColorData.speed, 1f );

			var easedTime = fadeColorData.easing.Value.Sample( fadeColorData.time );
			targetColorData.baseColor = math.lerp( matchableColor.value, neutralColor, easedTime );
			targetColorData.emission = math.lerp( matchableColor.emission, 0f, easedTime );

			if( fadeColorData.time == 1f ) {
				matchableColor.value = neutralColor;
				matchableColor.emission = 0f;
				matchableColorChanged.ValueRW = true;
				fadeColorEnabled.ValueRW = false;
			}
		}
	}
}