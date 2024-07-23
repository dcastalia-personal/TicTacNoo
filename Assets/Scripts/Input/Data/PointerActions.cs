using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

public class PointerActions : MonoBehaviour {
	public PhysicsCategoryTags pointerEventsBelongTo;
	public PhysicsCategoryTags pointerEventsCollideWith;
	
	public class Baker : Baker<PointerActions> {

		public override void Bake( PointerActions auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			
			AddComponent<PointerPressed>( self ); SetComponentEnabled<PointerPressed>( self, false );
			AddComponent<LastPointerPress>( self );
			AddComponent<PointerReleased>( self ); SetComponentEnabled<PointerReleased>( self, false );
			AddComponent<PointerHeld>( self ); SetComponentEnabled<PointerHeld>( self, false );
			
			AddComponent( self, new Input { raycastFilter = new CollisionFilter { BelongsTo = auth.pointerEventsBelongTo.Value, CollidesWith = auth.pointerEventsCollideWith.Value }} );
		}
	}
}

public struct Input : IComponentData {
	public CollisionFilter raycastFilter;
}

public struct PointerPressed : IComponentData, IEnableableComponent {
	public float3 worldPos;
	public float3 direction;
	public float2 screenPos;
	public float2 viewportPos;
}

public struct LastPointerPress : IComponentData {
	public float3 origin;
	public float3 direction;
	public float2 screenPos;
}

public struct PointerReleased : IComponentData, IEnableableComponent {
	public float3 worldPos;
	public float3 direction;
}

public struct PointerHeld : IComponentData, IEnableableComponent {
	public float3 worldPos;
	public float3 direction;
	public float2 screenDelta;
	public float2 screenPos;
	public float2 worldDelta;
	public float2 viewportPos;
}