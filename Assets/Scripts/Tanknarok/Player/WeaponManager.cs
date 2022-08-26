using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace FishNetworking.Tanknarok
{
	public class WeaponManager : NetworkBehaviour
	{
		public enum WeaponInstallationType
		{
			PRIMARY,
			SECONDARY,
			BUFF
		};
		
		[SerializeField] private Weapon[] _weapons;
		[SerializeField] private Player _player;
		
		private float _primaryIntervalWeapon;
		private float _secondaryIntervalWeapon;

		[SyncVar] public byte selectedPrimaryWeapon ;
		[SyncVar] public byte selectedSecondaryWeapon ;
		[SyncVar] public byte primaryAmmo ;
		[SyncVar] public byte secondaryAmmo ;
		private byte _activePrimaryWeapon;
		private byte _activeSecondaryWeapon;

		private void Update()
		{
			Render();
			TimeCountInterval();
		}

		public void Render()
		{
			ShowAndHideWeapons();
		}
		
		[Server]
		public void TimeCountInterval()
		{
			if (_primaryIntervalWeapon > 0)
				_primaryIntervalWeapon -= Time.deltaTime;

			if (_secondaryIntervalWeapon > 0)
				_secondaryIntervalWeapon -= Time.deltaTime;
		}

		private void ShowAndHideWeapons()
		{
			// Animates the scale of the weapon based on its active status
			for (int i = 0; i < _weapons.Length; i++)
			{
				_weapons[i].Show(i == selectedPrimaryWeapon || i == selectedSecondaryWeapon);
			}

			SetWeaponActive(selectedPrimaryWeapon, ref _activePrimaryWeapon);
			SetWeaponActive(selectedSecondaryWeapon, ref _activeSecondaryWeapon);
		}

		void SetWeaponActive(byte selectedWeapon, ref byte _activeWeapon)
		{
			if (_weapons[selectedWeapon].isShowing)
				_activeWeapon = selectedWeapon;
		}

		public void ActivateWeapon(WeaponInstallationType weaponType, int weaponIndex)
		{
			byte selectedWeapon = weaponType == WeaponInstallationType.PRIMARY ? selectedPrimaryWeapon : selectedSecondaryWeapon;
			byte activeWeapon = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;

			if (!_player.isActivated || selectedWeapon != activeWeapon)
				return;

			// Fail safe, clamp the weapon index within weapons list bounds
			weaponIndex = Mathf.Clamp(weaponIndex, 0, _weapons.Length - 1);

			if (weaponType == WeaponInstallationType.PRIMARY)
			{
				selectedPrimaryWeapon = (byte)weaponIndex;
				primaryAmmo = _weapons[(byte) weaponIndex].ammo;
			}
			else
			{
				selectedSecondaryWeapon = (byte)weaponIndex;
				secondaryAmmo = _weapons[(byte) weaponIndex].ammo;
			}
		}

		public void OnFireWeapon(WeaponInstallationType weaponType)
		{
			FireWeapon(weaponType);
		}
		[ServerRpc]
		public void FireWeapon(WeaponInstallationType weaponType)
		{
			if (!IsWeaponFireAllowed(weaponType))
				return;
			// Check interval weapon to allow fire
			float interval = weaponType switch
			{
				WeaponInstallationType.PRIMARY => _primaryIntervalWeapon,
				WeaponInstallationType.SECONDARY => _secondaryIntervalWeapon,
			};

			if (interval > 0) return;
			
			// Get ammo
			byte ammo = weaponType == WeaponInstallationType.PRIMARY ? primaryAmmo : secondaryAmmo;

			
			// Get weapon
			byte weaponIndex = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;
			Weapon weapon = _weapons[weaponIndex];
			if (!weapon.infiniteAmmo)
				ammo--;
			
			if (weaponType == WeaponInstallationType.PRIMARY)
				primaryAmmo = ammo;
			else
				secondaryAmmo = ammo;
			if (ammo == 0)
				
				ResetWeapon(weaponType);
			else
			{
				if (weaponType == WeaponInstallationType.PRIMARY)
					_primaryIntervalWeapon = _weapons[_activePrimaryWeapon].delay;
				else 
					_secondaryIntervalWeapon = _weapons[_activeSecondaryWeapon].delay;
			}
			weapon.Fire(base.Owner, _player.velocity);
		}

		private bool IsWeaponFireAllowed(WeaponInstallationType weaponType)
		{
			if (!_player.isActivated)
				return false;

			// Has the selected weapon become fully visible yet? If not, don't allow shooting
			if (weaponType == WeaponInstallationType.PRIMARY && _activePrimaryWeapon != selectedPrimaryWeapon)
				return false;
			else if (weaponType == WeaponInstallationType.SECONDARY && _activeSecondaryWeapon != selectedSecondaryWeapon)
				return false;
			return true;
		}
		
		[ServerRpc]
		public void ResetAllWeapons()
		{
			ResetWeapon(WeaponInstallationType.PRIMARY);
			ResetWeapon(WeaponInstallationType.SECONDARY);
		}

		void ResetWeapon(WeaponInstallationType weaponType)
		{
			if (weaponType == WeaponInstallationType.PRIMARY)
			{
				ActivateWeapon(weaponType, 0);
			}
			else if (weaponType == WeaponInstallationType.SECONDARY)
			{
				ActivateWeapon(weaponType, 4);
			}
		}

		public void InstallWeapon(PowerupElement powerup)
		{
			int weaponIndex = GetWeaponIndex(powerup.powerupType);
			ActivateWeapon(powerup.weaponInstallationType, weaponIndex);
		}

		private int GetWeaponIndex(PowerupType powerupType)
		{
			for (int i = 0; i < _weapons.Length; i++)
			{
				if (_weapons[i].powerupType == powerupType)
					return i;
			}

			Debug.LogError($"Weapon {powerupType} was not found in the weapon list, returning <color=red>0 </color>");
			return 0;
		}
	}
}