using Unity.Entities;
using UnityEngine;

public class LogOnInit : MonoBehaviour {

	public class Baker : Baker<LogOnInit> {

		public override void Bake( LogOnInit auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LogOnInitData {} );
		}
	}
}

public struct LogOnInitData : IComponentData {
	
}