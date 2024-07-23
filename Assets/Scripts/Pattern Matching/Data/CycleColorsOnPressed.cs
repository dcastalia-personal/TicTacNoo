using Unity.Entities;
using UnityEngine;

public class CycleColorsOnPressed : MonoBehaviour {

	public class Baker : Baker<CycleColorsOnPressed> {

		public override void Bake( CycleColorsOnPressed auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SetTargetColorByIndexData() ); SetComponentEnabled<SetTargetColorByIndexData>( self, false );
		}
	}
}

public struct SetTargetColorByIndexData : IComponentData, IEnableableComponent {}