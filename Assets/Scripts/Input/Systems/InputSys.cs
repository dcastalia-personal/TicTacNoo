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
	public const string ptrPressInput = "Pointer Press";
	public const string ptrPosInput = "Pointer Position";
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Input>();
	}

	public void OnUpdate( ref SystemState state ) {
		var raycaster = GetSingletonEntity<Input>();
		
		if( InputSystem.actions[ptrPressInput].IsPressed() ) {
			var pointerPos = InputSystem.actions[ptrPosInput].ReadValue<Vector2>();
			var camera = Camera.main;
			if( !camera ) return;
			var ray = camera.ScreenPointToRay( pointerPos );

			var delta = float2.zero;
			var viewportPos = camera.ScreenToViewportPoint( pointerPos );
			
			if( InputSystem.actions[ptrPressInput].WasPressedThisFrame() ) {
				SetComponentEnabled<PointerPressed>( raycaster, true );
				SetComponent( raycaster, new PointerPressed { worldPos = ray.origin, direction = ray.direction, screenPos = pointerPos, viewportPos = (Vector2)viewportPos } );
				SetComponent( raycaster, new LastPointerPress { origin = ray.origin, direction = ray.direction, screenPos = pointerPos} );
				SetComponentEnabled<PointerHeld>( raycaster, true );
			}
			
			if( HasComponent<PointerHeld>( raycaster ) ) {
				var prevPos = GetComponent<PointerHeld>( raycaster ).screenPos;
				delta = (float2)pointerPos - prevPos;
			}

			var screenDelta = Pointer.current.delta.ReadValue() * PlayerPrefs.GetFloat( "Sensitivity", 1f );
			var viewportDelta = new Vector3( delta.x / Screen.width, delta.y / Screen.height );
			var worldDelta = new float2( viewportDelta.x * camera.orthographicSize * 2f * camera.aspect, viewportDelta.y * camera.orthographicSize * 2f );
			
			SetComponent( raycaster, new PointerHeld { worldPos = ray.origin, direction = ray.direction, 
				screenDelta = screenDelta, worldDelta = worldDelta, screenPos = pointerPos, viewportPos = (Vector2)viewportPos } );
		}
		
		if( InputSystem.actions[ptrPressInput].WasReleasedThisFrame() ) {
			var pointerPos = InputSystem.actions[ptrPosInput].ReadValue<Vector2>();
			var camera = Camera.main;
			if( !camera ) return;
			var ray = camera.ScreenPointToRay( pointerPos );
			
			SetComponentEnabled<PointerHeld>( raycaster, false );
			SetComponentEnabled<PointerReleased>( raycaster, true );
			SetComponent( raycaster, new PointerReleased { worldPos = ray.origin,screenPos = pointerPos, direction = ray.direction } );
		}
	}
}

[UpdateAfter(typeof(CollectInput))]
[BurstCompile] public partial struct ProcessPointerEvents : ISystem {

	EntityQuery pressedQuery;
	EntityQuery releasedQuery;
	EntityQuery tappedQuery;
	EntityQuery heldQuery;
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Input>();
		state.RequireForUpdate<PhysicsWorldSingleton>();
		
		pressedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Pressed>() );
		releasedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Released>() );
		tappedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Tapped>() );
		heldQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Held>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
		var inputSingleton = GetSingletonEntity<Input>();
		var inputData = GetComponent<Input>( inputSingleton );

		// disable input tags from the previous frame
		state.EntityManager.SetComponentEnabled<Pressed>( pressedQuery, false );
		state.EntityManager.SetComponentEnabled<Released>( releasedQuery, false );
		state.EntityManager.SetComponentEnabled<Tapped>( tappedQuery, false );
		
		// enable any necessary input tags for this frame
		if( IsComponentEnabled<PointerPressed>( inputSingleton ) ) {
			var pointerPress = GetComponent<PointerPressed>( inputSingleton );

			var ray = new RaycastInput {
				Start = pointerPress.worldPos, 
				End = pointerPress.worldPos + pointerPress.direction * inputData.inputDistance,
				Filter = inputData.raycastFilter
			};
		
			collisionWorld.CastRay( ray, out RaycastHit hit );
		
			if( hit.Entity != Entity.Null ) {
				if( !HasComponent<Pressed>( hit.Entity ) ) return;
				SetComponentEnabled<Pressed>( hit.Entity, true );
				SetComponentEnabled<Held>( hit.Entity, true );

				SetComponent( hit.Entity, new Pressed { worldPos = hit.Position } );
				SetComponent( inputSingleton, new PointerTarget { value = hit.Entity } );
				SetComponentEnabled<PointerTarget>( inputSingleton, true );
			}
			else {
				SetComponentEnabled<PointerTarget>( inputSingleton, false );
			}
		}

		if( IsComponentEnabled<PointerHeld>( inputSingleton ) ) {
			var pointerHeld = GetComponent<PointerHeld>( inputSingleton );

			var ray = new RaycastInput {
				Start = pointerHeld.worldPos, 
				End = pointerHeld.worldPos + pointerHeld.direction * inputData.inputDistance,
				Filter = inputData.raycastFilter
			};
		
			collisionWorld.CastRay( ray, out RaycastHit hit );
			
			if( hit.Entity != Entity.Null ) {
				if( !HasComponent<Held>( hit.Entity ) ) return;

				SetComponent( hit.Entity, new Held { worldPos = hit.Position } );
				SetComponent( inputSingleton, new PointerTarget { value = hit.Entity } );
				SetComponentEnabled<PointerTarget>( inputSingleton, true );
			}
			else {
				SetComponentEnabled<PointerTarget>( inputSingleton, false );
			}
		}
		
		if( IsComponentEnabled<PointerReleased>( inputSingleton ) ) {
			var pointerRelease = GetComponent<PointerReleased>( inputSingleton );
		
			foreach( var (transform, held, self) in Query<RefRO<LocalTransform>, RefRO<Held>>().WithEntityAccess() ) {
				SetComponentEnabled<Released>( self, true );
				SetComponentEnabled<Held>( self, false );

				// determine if this was an up-and-down tap in the same general screen space location
				const float tappedDistThresholdSq = 16f;
				var pointerPress = GetComponent<PointerPressed>( inputSingleton );
				var distPressToReleaseSq = math.distancesq( pointerPress.screenPos, pointerRelease.screenPos );
				if( distPressToReleaseSq < tappedDistThresholdSq ) {
					SetComponentEnabled<Tapped>( self, true );
					SetComponent( self, new Tapped { worldPos = held.ValueRO.worldPos } );
				}

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