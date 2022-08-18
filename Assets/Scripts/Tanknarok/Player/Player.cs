using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public class Player : MonoBehaviour
    {
        public const byte MAX_HEALTH = 100;

        [Header("Visuals")]
        [SerializeField] private Transform _hull;
        [SerializeField] private Transform _turret;
        [SerializeField] private Transform _visualParent;
        [SerializeField] private Material[] _playerMaterials;
        [SerializeField] private TankTeleportInEffect _teleportIn;
        [SerializeField] private TankTeleportOutEffect _teleportOut;

        [Space(10)]
        [SerializeField] private GameObject _deathExplosionPrefab;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _pickupRadius;
        [SerializeField] private float _respawnTime;
        [SerializeField] private LayerMask _pickupMask;
        public State state { get; set; }
        public enum State
        {
            New,
            Despawned,
            Spawning,
            Active,
            Dead
        }
        public bool isActivated => (gameObject.activeInHierarchy && (state == State.Active || state == State.Spawning));
        public bool isDead => state == State.Dead;
        public Material playerMaterial { get; set; }

        public Color playerColor => playerMaterial.GetColor("_EnergyColor");
        public int playerID { get; private set; }
        public Vector3 turretPosition => _turret.position;

        public Quaternion turretRotation => _turret.rotation;

        public Quaternion hullRotation => _hull.rotation;
        private Collider[] _overlaps = new Collider[1];
        private Collider _collider;
        private LevelManager _levelManager;
        private float _respawnInSeconds = -1;
    }

}
