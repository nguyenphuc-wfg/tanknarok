using System;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
	public class HitScan : Projectile
	{
		public interface IVisual
		{
			void Activate(Vector3 origin, Vector3 target, bool impact);
			void Deactivate();
			bool IsActive();
		}

		private float currentLiveTime = 0;
		[SerializeField] private HitScanSettings _settings;
		
		[Serializable]
		public class HitScanSettings
		{
			public LayerMask hitMask;
			public float range = 100f;
			public float timeToFade = 1f;

			//Area of effect
			public float areaRadius;
			public float areaImpulse;
			public byte damage;
		}

		[SyncVar] public Vector3 networkedEndPosition;
		private Vector3 _predictedEndPosition;
		public Vector3 endPosition
		{
			get => networkedEndPosition;
			set { networkedEndPosition = value; }
		}

		[SyncVar] public Vector3 networkedStartPosition;
		private Vector3 _predictedStartPosition;
		public Vector3 startPosition
		{
			get => networkedStartPosition;
			set { networkedStartPosition = value; }
		}

		[SyncVar] public bool networkedImpact;
		private bool _predictedImpact;
		public bool impact
		{
			get => NetworkObject.IsSpawned ? _predictedImpact : networkedImpact;
			set { if (NetworkObject.IsSpawned) _predictedImpact = value;else networkedImpact = value; }
		}
		
		private IVisual _visual;

		private void Awake()
		{
			_visual = GetComponentInChildren<IVisual>(true);
		}

		public override void InitNetworkState(Vector3 ownerVelocity)
		{
			Debug.Log($"Initialising InstantHit predictedspawn");

			Transform exit = transform;
			
			RaycastHit hit;
			impact = Physics.Raycast(exit.position - 0.5f* exit.forward, exit.forward, out hit, _settings.range, _settings.hitMask.value);
			Vector3 hitPoint = exit.position + _settings.range * exit.forward;
			if (impact)
			{
				Debug.Log("Hitscan impact : " + hit.collider.gameObject.name);
				hitPoint = hit.point;
			}

			startPosition = transform.position;
			endPosition = hitPoint;
			ApplyAreaDamage(endPosition);
		}

		public void OnDestroy()
		{
			_visual.Deactivate();
		}

		public void FixedUpdate()
		{
			if (_visual == null) return;
			if (!_visual.IsActive())
				_visual.Activate(startPosition, endPosition, impact);
		}

		private void Update()
		{
			CountDownLiveTime();
		}
		
		[Server]
		private void CountDownLiveTime()
		{
			currentLiveTime += Time.deltaTime;
			if (currentLiveTime >= _settings.timeToFade)
			{
				currentLiveTime = 0;
				base.Despawn(this.NetworkObject);
			}
		}
		private void ApplyAreaDamage(Vector3 impactPos)
		{
			Collider[] hitColliders = Physics.OverlapSphere(impactPos, _settings.areaRadius,_settings.hitMask);
			foreach (var hitCollider in hitColliders)
			{
				GameObject other = hitCollider.gameObject;
				if (!other) return;
				ICanTakeDamage target = other.GetComponent<ICanTakeDamage>();
				if (target == null) return; 

				
				Vector3 impulse = other.transform.position - impactPos;
				float l = Mathf.Clamp(_settings.areaRadius - impulse.magnitude, 0, _settings.areaRadius);
				impulse = _settings.areaImpulse * l * impulse.normalized;
				target.ApplyDamage(impulse, _settings.damage, base.Owner);
			}
		}
	}
}