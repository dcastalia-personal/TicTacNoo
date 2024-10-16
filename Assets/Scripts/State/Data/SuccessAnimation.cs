using Unity.Entities;
using UnityEngine;

public class SuccessAnimation : MonoBehaviour {
	public float startMoveSpeed;
	public float acceleration;
	public float startRotSpeed;
	public float angAcceleration;
	public float flareStartTime;
	public float flareDuration;
	public float postFlareDuration;

	public GameObject flare;
	public SharedCurve flareEasing;

	public GameObject startAudio;
	public GameObject endAudio;

	public GameObject starParticle;

	public class Baker : Baker<SuccessAnimation> {

		public override void Bake( SuccessAnimation auth ) {
			var blobAssetRef = CurveBlob.CreateCurveBlob( auth.flareEasing );
			AddBlobAsset( ref blobAssetRef, out _ );
			
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SuccessAnimData {
				moveSpeed = auth.startMoveSpeed,
				acceleration = auth.acceleration,
				rotSpeed = auth.startRotSpeed, 
				angAcceleration = auth.angAcceleration, 
				flare = GetEntity( auth.flare, TransformUsageFlags.Dynamic ),
				flareDuration = auth.flareDuration,
				flareStartTime = auth.flareStartTime,
				postFlareDuration = auth.postFlareDuration,
				easing = blobAssetRef,
				startAnimAudio = GetEntity( auth.startAudio, TransformUsageFlags.Dynamic ),
				endAnimAudio = GetEntity( auth.endAudio, TransformUsageFlags.Dynamic ),
				particlePrefab = GetEntity( auth.starParticle, TransformUsageFlags.Dynamic ),
			} );
		}
	}
}

public struct SuccessAnimData : IComponentData {
	public float timeElapsed;
	public float moveSpeed;
	public float acceleration;
	public float rotSpeed; // in radians
	public float angAcceleration;

	public Entity flare;
	public Entity particlePrefab;

	public float flareStartTime;
	public float flareDuration;
	public float postFlareDuration;

	public bool flared;
	public bool particlesSpawned;
	
	public BlobAssetReference<CurveBlob> easing;

	public Entity startAnimAudio;
	public Entity endAnimAudio;
}