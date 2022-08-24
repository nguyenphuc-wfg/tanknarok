using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public class TankTeleportOutEffect : MonoBehaviour
    {
        [SerializeField] private Player _player;

        [SerializeField] private GameObject _dummyTank;
        [SerializeField] private Transform _dummyTankTurret;
        [SerializeField] private Transform _dummyTankHull;

        [SerializeField] private ParticleSystem _teleportEffect;

        [Header("Audio")][SerializeField] private AudioEmitter _audioEmitter;

        // Initialize dummy tank and set colors based on the assigned player
        public void Initialize()
        {
            // _player = player;

            _dummyTank.SetActive(false);

            ColorChanger.ChangeColor(transform, _player.playerColor);

            _teleportEffect.Stop();
        }

        public void StartTeleport()
        {
            if (_audioEmitter.isActiveAndEnabled)
                _audioEmitter.PlayOneShot();

            transform.position = _player.transform.position;

            _dummyTank.SetActive(false);
            _dummyTank.SetActive(true);

            _dummyTankTurret.rotation = _player.turretRotation;
            _dummyTankHull.rotation = _player.hullRotation;

            _teleportEffect.Play();
        }
    }
}