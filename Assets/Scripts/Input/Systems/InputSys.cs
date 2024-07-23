using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Entities.SystemAPI;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateAfter(typeof(ReleasePointerEventsSys))]
public partial struct CollectInput : ISystem {
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Input>();
	}

	public void OnUpdate( ref SystemState state ) {
		var raycaster = GetSingletonEntity<Input>();
		
		if( Mouse.current.leftButton.IsPressed() ) {
			var pointerPos = Mouse.current.position.ReadValue();
			var camera = Camera.main;
			if( !camera ) return;
			var ray = camera.ScreenPointToRay( pointerPos );

			var delta = float2.zero;
			var viewportPos = camera.ScreenToViewportPoint( pointerPos );
			
			if( Mouse.current.leftButton.wasPressedThisFrame ) {
				SetComponentEnabled<PointerPressed>( raycaster, true );
				SetComponent( raycaster, new PointerPressed { worldPos = ray.origin, direction = ray.direction, screenPos = pointerPos, viewportPos = (Vector2)viewportPos } );
				SetComponent( raycaster, new LastPointerPress { origin = ray.origin, direction = ray.direction, screenPos = pointerPos} );
				SetComponentEnabled<PointerHeld>( raycaster, true );
			}
			
			if( HasComponent<PointerHeld>( raycaster ) ) {
				var prevPos = GetComponent<PointerHeld>( raycaster ).screenPos;
				delta = (float2)pointerPos - prevPos;
			}

			var screenDelta = Mouse.current.delta.ReadValue();
			var viewportDelta = new Vector3( delta.x / Screen.width, delta.y / Screen.height );
			var worldDelta = new float2( viewportDelta.x * camera.orthographicSize * 2f * camera.aspect, viewportDelta.y * camera.orthographicSize * 2f );
			
			SetComponent( raycaster, new PointerHeld { worldPos = ray.origin, direction = ray.direction, 
				screenDelta = screenDelta, worldDelta = worldDelta, screenPos = pointerPos, viewportPos = (Vector2)viewportPos } );
		}
		
		if( Mouse.current.leftButton.wasReleasedThisFrame ) {
			var pointerPos = Mouse.current.position.ReadValue();
			var camera = Camera.main;
			if( !camera ) return;
			var ray = camera.ScreenPointToRay( pointerPos );
			
			SetComponentEnabled<PointerHeld>( raycaster, false );
			SetComponentEnabled<PointerReleased>( raycaster, true );
			SetComponent( raycaster, new PointerReleased { worldPos = ray.origin, direction = ray.direction } );
		}
	}
}

[UpdateAfter(typeof(CollectInput))]
[BurstCompile] public partial struct ProcessPointerEvents : ISystem {

	EntityQuery pressedQuery;
	EntityQuery releasedQuery;
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Input>();
		state.RequireForUpdate<PhysicsWorldSingleton>();
		
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Pressed>() );
		releasedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Released>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
		var inputSingleton = GetSingletonEntity<Input>();
		var inputData = GetComponent<Input>( inputSingleton );

		// disable input tags from the previous frame
		state.EntityManager.SetComponentEnabled<Pressed>( pressedQuery, false );
		state.EntityManager.SetComponentEnabled<Released>( releasedQuery, false );
		
		// enable any necessary input tags for this frame
		if( IsComponentEnabled<PointerPressed>( inputSingleton ) ) {
			var pointerPress = GetComponent<PointerPressed>( inputSingleton );

			const float rayLength = 20f;
			var ray = new RaycastInput {
				Start = pointerPress.worldPos, 
				End = pointerPress.worldPos + pointerPress.direction * rayLength,
				Filter = inputData.raycastFilter
			};
		
			collisionWorld.CastRay( ray, out RaycastHit hit );
		
			if( hit.Entity != Entity.Null ) {
				if( !HasComponent<Pressed>( hit.Entity ) ) return;
				SetComponentEnabled<Pressed>( hit.Entity, true );
				SetComponentEnabled<Held>( hit.Entity, true );

				SetComponent( hit.Entity, new Pressed { worldPos = hit.Position } );
			}
		}

		if( IsComponentEnabled<PointerReleased>( inputSingleton ) ) {
			var pointerRelease = GetComponent<PointerReleased>( inputSingleton );
		
			foreach( var (transform, self) in Query<RefRO<LocalTransform>>().WithAll<Held>().WithEntityAccess() ) {
				SetComponentEnabled<Released>( self, true );
				SetComponentEnabled<Held>( self, false );

				SetComponent( self, new Released { worldPos = new float3( pointerRelease.worldPos.xy, transform.ValueRO.Position.z ) } );
			}
		}
	}
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true )]
public partial struct ReleasePointerEventsSys : ISystem {

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Input>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var inputSingleton = GetSingletonEntity<Input>();
		SetComponentEnabled<PointerPressed>( inputSingleton, false );
		SetComponentEnabled<PointerReleased>( inputSingleton, false );
	}
}