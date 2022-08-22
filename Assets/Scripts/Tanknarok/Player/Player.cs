using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Connection;

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
    
        [SyncVar(Channel = Channel.Unreliable, OnChange = nameof(OnStateChanged))]
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

        // public void ToggleReady()
        // {
        //     ready = !ready;
        // }

        // public void ResetReady()
        // {
        //     ready = false;
        // }
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

            playerID = base.ObjectId;
            ready = false;

            SetMaterial(playerID % 4);
            SetupDeathExplosion();

            _damageVisuals.Initialize(playerMaterial);

            _teleportIn.Initialize(this);
            _teleportOut.Initialize(this);

            _cc.enabled = base.IsOwner;
            PlayerManager.AddPlayer(this);
        }
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            state = State.Despawned;
            PlayerManager.RemovePlayer(this);
        }
        private void OnDestroy()
        {
        }
        private void Update()
        {
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
            state = State.Spawning;
            // StateChanged();
            lives = maxLives;
            life = MAX_HEALTH;
            score = 0;
        }
    
        private void OnStateChanged(State prev, State next, bool asServer)
        {
            //Debug.Log($" OnStateChanged: {prev} to {next}");
            //if (!asServer)
                StateChanged();
        }
        
        public void StateChanged()
        {
            switch (state)
            {
                case State.Spawning:
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
        private LevelManager GetLevelManager()
        {
            if (_levelManager == null)
                _levelManager = FindObjectOfType<LevelManager>();
            return _levelManager;
        }
        public void Render()
        {
            _visualParent.gameObject.SetActive(state == State.Active);
            _collider.enabled = state != State.Dead;
            // _damageVisuals.CheckHealth(life);

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
            // To prevent the tank from making a 180 degree turn every time we reverse the movement direction
            // we define a driving direction that creates a multiplier for the hull.forward. This allows us to
            // drive "backwards" as well as "forwards"
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
        public void ApplyDamage(Vector3 impulse, byte damage, NetworkConnection attacker)
        {
            Debug.Log("bi damage");
            return;
            // if (!isActivated || !invulnerabilityTimer.Expired(Runner))
            //     return;

            //Don't damage yourself
            // Player attackingPlayer = PlayerManager.Get(attacker);
            // if (attackingPlayer != null && attackingPlayer.playerID == playerID)
            //     return;

            // ApplyImpulse(impulse);

            if (damage >= life)
            {
                life = 0;
                state = State.Dead;
				
                // if(GameManager.playState==GameManager.PlayState.LEVEL)
                //     lives -= 1;

                if (lives > 0)
                    Respawn( _respawnTime );

                // GameManager.instance.OnTankDeath();
            }
            else
            {
                life -= damage;
                Debug.Log($"Player {playerID} took {damage} damage, life = {life}");
            }

            // invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);

            // if (Runner.Stage == SimulationStages.Forward)
                // _damageVisuals.OnDamaged(life, isDead);
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
