using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitializationSystemGroup) )] [UpdateAfter( typeof(SceneSystemGroup) )]
public partial struct InitStarParticleSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StarParticleData, RequireInitData>().WithPresent<ScaleOnCurveData>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitStarParticleJob {}.ScheduleParallel( query );
	}

	[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	[BurstCompile] partial struct InitStarParticleJob : IJobEntity {
		void Execute( in StarParticleData starParticleData, EnabledRefRW<ScaleOnCurveData> scaleOnCurveEnabled ) {
			scaleOnCurveEnabled.ValueRW = true;
		}
	}
}

public partial struct MoveStarParticleSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StarParticleData, RandomDirectionData, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var camEntity = GetSingletonEntity<CameraData>();
		new MoveStarParticleJob { deltaTime = SystemAPI.Time.DeltaTime, camLTW = GetComponent<LocalToWorld>( camEntity ) }.ScheduleParallel( query );
	}

	[BurstCompile] partial struct MoveStarParticleJob : IJobEntity {
		public float deltaTime;
		public LocalToWorld camLTW;
		
		void Execute( in StarParticleData starParticleData, in RandomDirectionData direction, ref LocalTransform transform ) {
			transform = transform.Translate( transform.InverseTransformDirection( starParticleData.speed * deltaTime * direction.value ) );
		}
	}
}