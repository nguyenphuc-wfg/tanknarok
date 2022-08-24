using System.Threading.Tasks;
using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Connection;
using FishNetworking.Event;
using UnityEngine.Events;

namespace FishNetworking.Tanknarok
{
    public class Player : NetworkBehaviour, ICanTakeDamage
    {
        public const byte MAX_HEALTH = 100;
        [SerializeField] private float _speed = 5f;
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
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private CharacterController _cc;
        [SerializeField] private TankDamageVisual _damageVisuals;

        public Vector3 velocity => _cc.velocity;

        [SyncVar] public byte life;

        [SyncVar] public string playerName;

        [SyncVar] public Vector2 moveDirection;

        [SyncVar] public Vector2 aimDirection;

        [SyncVar] public byte lives;
        
        [SyncVar] public byte score;
        [SyncVar] public bool ready;
        public static Player local { get; set; }
    
        [SyncVar(Channel = Channel.Reliable,OnChange = nameof(OnStateChanged))]
        [SerializeField]
        private State state;
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
        public bool isRespawningDone => state == State.Spawning;
        public Material playerMaterial { get; set; }

        public Color playerColor => playerMaterial.GetColor("_EnergyColor");
        public WeaponManager shooter => weaponManager;
        public int playerID { get; set; }
        public Vector3 turretPosition => _turret.position;
        public Quaternion turretRotation => _turret.rotation;
        public Quaternion hullRotation => _hull.rotation;
        private float _gravity;

        enum DriveDirection
        {
            FORWARD,
            BACKWARD
        };
        private DriveDirection _driveDirection = DriveDirection.FORWARD;
        private Collider[] _overlaps = new Collider[1];
        [SerializeField] private Collider _collider;
        private LevelManager _levelManager;
        private Vector2 _lastMoveDirection; // Store the previous direction for correct hull rotation
        private GameObject _deathExplosionInstance;
        private float _respawnInSeconds = -1;
        public UnityEvent tankDeathEvent;
        public void ToggleReady()
        {
            ready = !ready;
        }

        public void ResetReady()
        {
            ready = false;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            OnStartClient();
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsOwner)
                local = this;

            InitNetworkState(3);
            PlayerManager.AddPlayer(this);
            playerID = base.OwnerId;
            ready = false;

            SetMaterial(playerID % 4);
            SetupDeathExplosion();

            _damageVisuals.Initialize(playerMaterial);

            _teleportIn.Initialize();
            _teleportOut.Initialize();

            _cc.enabled = base.IsOwner;

        }
        private void FixedUpdate()
        {
            CheckForPowerupPickup();
        }
        private void Update()
        {
            if (_respawnInSeconds >= 0)
                CheckRespawn();
            Render();
        }
        void SetupDeathExplosion()
        {
            _deathExplosionInstance = Instantiate(_deathExplosionPrefab, transform.parent);
            _deathExplosionInstance.SetActive(false);
            ColorChanger.ChangeColor(_deathExplosionInstance.transform, playerColor);
        }
        public void InitNetworkState(byte maxLives)
        {
            ResetState(maxLives);
            score = 0;
        }

        public void ResetState(byte maxLives)
        {
            state = State.Spawning;
            lives = maxLives;
            life = MAX_HEALTH;
        }
        private void CheckRespawn()
        {
            if (_respawnInSeconds > 0)
                _respawnInSeconds -= Time.deltaTime;
            InitRespawning();
        }

        private void InitRespawning()
        {
            if (_respawnInSeconds <= 0)
            {
                Debug.Log($"Respawning player {playerID}, life={life}, lives={lives}, from state={state}");
                // Make sure we don't get in here again, even if we hit exactly zero
                _respawnInSeconds = -1;

                // Restore health
                life = MAX_HEALTH;

                // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
                if (state != State.Active)
                    state = State.Spawning;
            }
        }
        private void OnStateChanged(State prev, State next, bool asServer)
        {
            // Debug.LogError($" OnStateChanged: {prev} to {next}");
            // if (!asServer)
            StateChanged();
        }

        private void SetSpawnPosition()
        {
            SpawnPoint spawnpt = GetLevelManager().GetPlayerSpawnPoint(playerID);
            if (spawnpt != null)
            {
                Transform spawn =spawnpt.transform;
                transform.position = spawn.position;
                transform.rotation = spawn.rotation;
            }
        }
        public void StateChanged()
        {
            switch (state)
            {
                case State.Spawning:
                    SetSpawnPosition();
                    _teleportIn.StartTeleport();
                    break;
                case State.Active:
                    _damageVisuals.CleanUpDebris();
                    _teleportIn.EndTeleport();
                    break;
                case State.Dead:
                    _deathExplosionInstance.transform.position = transform.position;
                    _deathExplosionInstance.SetActive(false); // dirty fix to reactivate the death explosion if the particlesystem is still active
                    _deathExplosionInstance.SetActive(true);
                    _visualParent.gameObject.SetActive(false);
                    _damageVisuals.OnDeath();
                    break;
                case State.Despawned:
                    _teleportOut.StartTeleport();
                    break;
            }
        }
        public void ResetPlayer()
        {
            Debug.Log($"Resetting player {playerID},to state={state}");
            shooter.ResetAllWeapons();
            state = State.Active;
        }
        public void OnDestroy()
        {
            Destroy(_deathExplosionInstance);
            PlayerManager.RemovePlayer(this);
        }
        public void DespawnTank()
        {
            if (state == State.Dead)
                return;

            state = State.Despawned;
        }
        [Server]
        public void Pickup(PowerupSpawner powerupSpawner)
        {
            if (!powerupSpawner)
                return;
            
            PowerupElement powerup = powerupSpawner.Pickup();
            if (powerup == null)
                return;

            if (powerup.powerupType == PowerupType.HEALTH)
                life = MAX_HEALTH;
            else
                shooter.InstallWeapon(powerup);
        }
        [Server]
        private void CheckForPowerupPickup()
        {
            // If we run into a powerup, pick it up
            if (isActivated)
            {
                _overlaps = Physics.OverlapSphere(transform.position, _pickupRadius,_pickupMask);
                if (_overlaps.Length > 0)
                    Pickup(_overlaps[0].GetComponent<PowerupSpawner>());
            }
        }
        private LevelManager GetLevelManager()
        {
            if (_levelManager == null)
                _levelManager = FindObjectOfType<LevelManager>(true);
            return _levelManager;
        }

        public void Render()
        {
            _visualParent.gameObject.SetActive(state == State.Active);
            _collider.enabled = state != State.Dead;
            _cc.enabled = state != State.Dead;
            _damageVisuals.CheckHealth(life);

            // Add a little visual-only movement to the mesh
            SetMeshOrientation();

            if (moveDirection.magnitude > 0.1f)
                _lastMoveDirection = moveDirection;
        }
    
        private void SetMaterial(int playerId)
        {
            playerMaterial = Instantiate(_playerMaterials[playerId]);
            TankPartMesh[] tankParts = GetComponentsInChildren<TankPartMesh>();
            foreach (TankPartMesh part in tankParts)
            {
                part.SetMaterial(playerMaterial);
            }
        }

        public void Respawn(float inSeconds)
        {
            Debug.Log("Respawn Player Tank Death");
            _respawnInSeconds = inSeconds;
        }

        public void SetDirections(Vector2 moveDirection, Vector2 aimDirection)
        {
            if (!_cc.enabled)
                return;
            this.moveDirection = moveDirection;
            this.aimDirection = aimDirection;
        }
        public void Move()
        {
            if (!_cc.enabled)
                return;
            if (!_cc.isGrounded)
                _gravity -= 9.8f;
            else
                _gravity = 0f;
            _cc.Move(new Vector3(moveDirection.x * Time.deltaTime * _speed, _gravity, moveDirection.y * Time.deltaTime * _speed));
        }
        private void SetMeshOrientation()
        {
            switch (_driveDirection)
            {
                case DriveDirection.FORWARD:
                    if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
                        _driveDirection = DriveDirection.BACKWARD;
                    break;
                case DriveDirection.BACKWARD:
                    if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
                        _driveDirection = DriveDirection.FORWARD;
                    break;
            }

            float multiplier = _driveDirection == DriveDirection.FORWARD ? 1 : -1;

            if (moveDirection.magnitude > 0.1f)
                _hull.forward = Vector3.Lerp(_hull.forward, new Vector3(moveDirection.x, 0, moveDirection.y) * multiplier, Time.deltaTime * 10f);

            if (aimDirection.sqrMagnitude > 0)
                _turret.forward = Vector3.Lerp(_turret.forward, new Vector3(aimDirection.x, 0, aimDirection.y), Time.deltaTime * 100f);
        }

        [ServerRpc]
        public void ApplyDamage(Vector3 impulse, byte damage, NetworkConnection attacker)
        {
            if (!isActivated)
                return;
            //Don't damage yourself
            Player attackingPlayer = PlayerManager.Get(attacker);
            if (attackingPlayer != null && attackingPlayer.playerID == playerID)
                return;
            RpcDamageVisual();
            OnDamage(impulse, damage, attacker);
        }
        
        // Visual Damage to client
        [ObserversRpc(RunLocally = true)]
        private void RpcDamageVisual()
        {
            _damageVisuals.OnDamaged(life, isDead);
        }
        
        // Server apply damage
        [Server]
        private void OnDamage(Vector3 impulse, byte damage, NetworkConnection attacker)
        {
            if (damage >= life)
            {
                life = 0;
                state = State.Dead;
				
                // if(GameManager.playState==GameManager.PlayState.LEVEL)
                    lives -= 1;

                if (lives > 0)
                    Respawn( _respawnTime );
                else
                    tankDeathEvent?.Invoke();
            }
            else
            {
                life -= damage;
                Debug.Log($"Player {playerID} took {damage} damage, life = {life}");
            }
        }
    }
    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public ReconcileData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

}
