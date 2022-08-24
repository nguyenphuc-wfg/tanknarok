using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FishNetworking.Tanknarok
{
	/// <summary>
	/// Powerups are spawned by the LevelManager and, when picked up, changes the
	/// current weapon of the tank.
	/// </summary>
	public class PowerupSpawner : NetworkBehaviour
	{
		[SerializeField] private PowerupElement[] _powerupElements;
		[SerializeField] private Renderer _renderer;
		[SerializeField] private MeshFilter _meshFilter;
		[SerializeField] private MeshRenderer _rechargeCircle;

		[Header("Colors")] 
		[SerializeField] private Color _mainPowerupColor;
		[SerializeField] private Color _specialPowerupColor;
		[SerializeField] private Color _buffPowerupColor;

		[SyncVar(Channel = Channel.Reliable, OnChange = nameof(OnRespawningChanged))]
		public bool isRespawning;

		[SyncVar(Channel = Channel.Reliable, OnChange = nameof(OnActivePowerupIndexChanged))]
		public int activePowerupIndex;

		[SyncVar]
		public float respawnTimerFloat;

		private float _respawnDuration = 3f;
		public float respawnProgress => respawnTimerFloat / _respawnDuration;
		void OnEnable()
		{
			SetRechargeAmount(0f);
		}
		[Server]
		public void Start()
		{
			_renderer.enabled = false;
			isRespawning = true;
			SetNextPowerup();
		}
		[Server]
		public void FixedUpdate()
		{
			// if (!Object.HasStateAuthority)
			// 	return;

			// Update the respawn timer
			respawnTimerFloat = Mathf.Min(respawnTimerFloat + Time.deltaTime, _respawnDuration);

			// Spawn a new powerup whenever the respawn duration has been reached
			if (respawnTimerFloat >= _respawnDuration && isRespawning)
			{
				isRespawning = false;
			}
		}
		
		private void Update()
		{
			Render();
		}

		// Create a simple scale in effect when spawning
		public void Render()
		{
			if (!isRespawning)
			{
				_renderer.transform.localScale = Vector3.Lerp(_renderer.transform.localScale, Vector3.one, Time.deltaTime * 5f);
			}
			else
			{
				_renderer.transform.localScale = Vector3.zero;
				SetRechargeAmount(respawnProgress);
			}
		}

		/// <summary>
		/// Get the pickup contained in this spawner and trigger the spawning of a new powerup
		/// </summary>
		/// <returns></returns>
		public PowerupElement Pickup()
		{
			if (isRespawning)
				return null;
			// Store the active powerup index for returning
			int lastIndex = activePowerupIndex;

			// Trigger the pickup effect, hide the powerup and select the next powerup to spawn
			if (respawnTimerFloat >= _respawnDuration)
			{
				if (_renderer.enabled)
				{
					GetComponent<AudioEmitter>().PlayOneShot(_powerupElements[lastIndex].pickupSnd);
					_renderer.enabled = false;
					SetNextPowerup();
				}
			}
			return lastIndex != -1 ? _powerupElements[lastIndex] : null;
		}
		[Server]
		private void SetNextPowerup()
		{
			activePowerupIndex = Random.Range(0, _powerupElements.Length);
			respawnTimerFloat = 0;
			isRespawning = true;
		}

		public void OnActivePowerupIndexChanged(int prev, int next, bool asServer)
		{
			RefreshColor();
		}
		//
		private void OnRespawningChanged(bool prev, bool next, bool asServer)
		{
			_renderer.enabled = true;
			_meshFilter.mesh = _powerupElements[activePowerupIndex].powerupSpawnerMesh;
			SetRechargeAmount(0);
		}

		private void RefreshColor()
		{
			if (_rechargeCircle != null)
			{
				Color respawnColor = _mainPowerupColor;
				switch (_powerupElements[activePowerupIndex].weaponInstallationType)
				{
					case WeaponManager.WeaponInstallationType.PRIMARY:
						respawnColor = _mainPowerupColor;
						break;
					case WeaponManager.WeaponInstallationType.SECONDARY:
						respawnColor = _specialPowerupColor;
						break;
					case WeaponManager.WeaponInstallationType.BUFF:
						respawnColor = _buffPowerupColor;
						break;
				}
				_rechargeCircle.material.color = respawnColor;
			}
		}

		public void SetRechargeAmount(float amount)
		{
			_rechargeCircle.material.SetFloat("_Recharge", amount);
		}
	}
}