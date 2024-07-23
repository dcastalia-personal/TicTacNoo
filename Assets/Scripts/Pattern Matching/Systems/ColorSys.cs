using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InitColor : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<TargetColorData, RequireInitData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (targetColorData, emissiveColorData) in Query<RefRO<TargetColorData>, RefRW<CurrentColorData>>().WithAll<RequireInitData>() ) {
			emissiveColorData.ValueRW.value = targetColorData.ValueRO.baseColor;
		}
	}
}

[UpdateBefore(typeof(EnableTargetColorOnPressed))]
public partial struct ClearTargetColorDataChangedSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<TargetColorDataChanged>().WithNone<PreventClearColorChanges>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		state.EntityManager.SetComponentEnabled<TargetColorDataChanged>( query, false );
	}
}

[UpdateBefore(typeof(SetTargetColorByIndexSys))] [UpdateAfter(typeof(ClearPlayerStepSys))]
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
		foreach( var (_, self) in Query<RefRO<TargetColorData>>().WithAll<Pressed>().WithEntityAccess() ) {
			if( HasComponent<SetTargetColorByIndexData>( self ) ) {
				SetComponentEnabled<SetTargetColorByIndexData>( self, true );

				var gameStateEntity = GetSingletonEntity<GameStateData>();
				SetComponentEnabled<PlayerStepped>( gameStateEntity, true );
			}
		}
	}
}

public partial struct SetTargetColorByIndexSys : ISystem {
	
	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (targetColorData, setByIndexEnabled, self) in Query<RefRW<TargetColorData>, EnabledRefRW<SetTargetColorByIndexData>>().WithEntityAccess() ) {
			var colorDirectory = GetSingletonBuffer<LevelColorData>();
			var colorIndex = GetSingletonRW<LevelColorIndex>();
			
			targetColorData.ValueRW.baseColor = colorDirectory[ colorIndex.ValueRO.value ].color;
			targetColorData.ValueRW.emission = colorDirectory[ colorIndex.ValueRO.value ].intensity;
			var newColorIndex = colorIndex.ValueRW.value = (colorIndex.ValueRO.value + 1) % colorDirectory.Length;
			colorIndex.ValueRW.value = newColorIndex;
			setByIndexEnabled.ValueRW = false;
			SetComponentEnabled<TargetColorDataChanged>( self, true );

			var cameraEntity = GetSingletonEntity<CameraData>();
			var cameraData = GetComponent<TargetColorData>( cameraEntity );
			cameraData.baseColor = colorDirectory[ newColorIndex ].backgroundColor;
			SetComponent( cameraEntity, cameraData );
			SetComponentEnabled<TargetColorData>( cameraEntity, true );
		}
	}
}

public partial struct UpdateColorSys : ISystem {
	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		new UpdateColorSysJob { deltaTime = deltaTime }.ScheduleParallel();
	}

	[WithAll( typeof(TargetColorData), typeof(CurrentColorData) )]
	[BurstCompile] partial struct UpdateColorSysJob : IJobEntity {
		public float deltaTime;
		
		void Execute( in TargetColorData targetColorData, ref CurrentColorData currentColorData, ref CurrentEmissionData currentEmissionData ) {
			currentColorData.value = math.lerp( currentColorData.value, targetColorData.baseColor, targetColorData.tweenSpeed * deltaTime );
			currentEmissionData.value = math.lerp( currentEmissionData.value, targetColorData.emission, targetColorData.tweenSpeed * deltaTime );
		}
	}
}