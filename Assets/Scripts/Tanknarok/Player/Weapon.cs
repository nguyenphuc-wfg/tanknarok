using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using FishNetworking.Utility;

namespace FishNetworking.Tanknarok
{
    public class Weapon : NetworkBehaviour
    {
        [SerializeField] private Transform[] _gunExits;
        [SerializeField] private GameObject _projectilePrefab; // Networked projectile
        [SerializeField] private float _rateOfFire;
        [SerializeField] private byte _ammo;
        [SerializeField] private bool _infiniteAmmo;
        [SerializeField] private AudioEmitter _audioEmitter;
        [SerializeField] private LaserSightLine _laserSight;
        [SerializeField] private PowerupType _powerupType = PowerupType.DEFAULT;
        [SerializeField] private ParticleSystem _muzzleFlashPrefab;

        private int _gunExit;
        private float _visible;
        private bool _active;
        private List<ParticleSystem> _muzzleFlashList = new List<ParticleSystem>();

        public float delay => _rateOfFire;
        public bool isShowing => _visible >= 1.0f;
        public byte ammo => _ammo;
        public bool infiniteAmmo => _infiniteAmmo;

        public PowerupType powerupType => _powerupType;
        private void Awake()
        {
            // Create a muzzle flash for each gun exit point the weapon has
            if (_muzzleFlashPrefab != null)
            {
                foreach (Transform gunExit in _gunExits)
                {
                    _muzzleFlashList.Add(Instantiate(_muzzleFlashPrefab, gunExit.position, gunExit.rotation, transform));
                }
            }
        }
        public void Show(bool show)
        {
            if (_active && !show)
            {
                ToggleActive(false);
            }
            else if (!_active && show)
            {
                ToggleActive(true);
            }

            _visible = Mathf.Clamp(_visible + (show ? Time.deltaTime : -Time.deltaTime) * 5f, 0, 1);

            if (show)
                transform.localScale = Tween.easeOutElastic(0, 1, _visible) * Vector3.one;
            else
                transform.localScale = Tween.easeInExpo(0, 1, _visible) * Vector3.one;
        }
        private void ToggleActive(bool value)
        {
            _active = value;

            if (_laserSight != null)
            {
                if (_active)
                {
                    _laserSight.SetDuration(0.5f);
                    _laserSight.Activate();
                }
                else
                    _laserSight.Deactivate();
            }
        }

        [ServerRpc(RunLocally = true)]
        public void Fire(NetworkConnection runner ,Vector3 ownerVelocity)
        {
            if (powerupType == PowerupType.EMPTY || _gunExits.Length == 0)
                return;
            Transform exit = GetExitPoint();
            SpawnNetworkShot(runner, ownerVelocity, exit);
            FireFx();
        }
        
        [ObserversRpc(IncludeOwner =  true)]
        public void FireFx()
        {
            OnFireFx();
        }

        public void OwerFireFx()
        {
            // Recharge the laser sight if this weapon has it
            OnFireFx();
        }

        private void OnFireFx()
        {
            if (_laserSight != null)
                _laserSight.Recharge();

            if(_gunExit<_muzzleFlashList.Count)
                _muzzleFlashList[_gunExit].Play();
            _audioEmitter.PlayOneShot(); 
        }
        
        private void SpawnNetworkShot(NetworkConnection runner ,Vector3 ownerVelocity,Transform exit)
        {
            GameObject bullet = Instantiate(_projectilePrefab, exit.position , exit.rotation);
            bullet.GetComponent<Projectile>().InitNetworkState(ownerVelocity);
            base.Spawn(bullet, runner);

        }
        private Transform GetExitPoint()
        {
            _gunExit = (_gunExit + 1) % _gunExits.Length;
            Transform exit = _gunExits[_gunExit];
            return exit;
        }
    }

}
