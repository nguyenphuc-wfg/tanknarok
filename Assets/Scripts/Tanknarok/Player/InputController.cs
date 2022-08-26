using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public struct ReconcileDataPlayer
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }
    public struct MoveDataPlayer
    {
        public Vector2 MoveDelta;
        public Vector2 AimDelta;
    }
    public class InputController : NetworkBehaviour
    {
        [SerializeField] private LayerMask _mouseRayMask;

        public static bool fetchInput = true;
        public bool ToggleReady { get; set; }

        [SerializeField] private Player _player;
        private Vector2 _moveDelta;
        private Vector2 _aimDelta;
        private Vector2 _leftPos;
        private Vector2 _leftDown;
        private Vector2 _rightPos;
        private Vector2 _rightDown;
        private bool _leftTouchWasDown;
        private bool _rightTouchWasDown;
        private bool _primaryFire;
        private bool _secondaryFire;

        private void Update()
        {
            if (!IsOwner) return;
            if (GameManager.playState == GameManager.PlayState.LOBBY)
                
            if (Input.GetKeyDown(KeyCode.R))
                _player.ToggleReady();
            
            if (!fetchInput) return;
            UpdateFire();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (base.IsServer || base.IsClient)
                base.TimeManager.OnTick += TimeManager_OnTick;
        }
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (base.TimeManager != null)
                base.TimeManager.OnTick -= TimeManager_OnTick;
        }
        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                Reconcile(default, false);
                InputMove(out MoveDataPlayer dt);
                SetDirectionPlayer(dt,false);
            }
            if (base.IsServer)
            {
                SetDirectionPlayer(default,true);
                ReconcileDataPlayer rd = new ReconcileDataPlayer()
                {
                    Position = transform.position,
                    Rotation = transform.rotation
                };
                Reconcile(rd, true);
            }
        }
        
        [Replicate]
        private void SetDirectionPlayer(MoveDataPlayer moveData ,bool asServer, bool replaying = false)
        {
            _player.SetDirections(moveData, asServer);
        }
        [ServerRpc]
        public void ReconcileTransform(ReconcileDataPlayer recData)
        {
            ReconcileTransformRpc(recData);
        }
        [ObserversRpc]
        public void ReconcileTransformRpc(ReconcileDataPlayer recData)
        {
            transform.position = recData.Position;
            transform.rotation = recData.Rotation;
        }
        [Reconcile]
        private void Reconcile(ReconcileDataPlayer recData, bool asServer)
        {
            ReconcileTransform(recData);
            transform.position = recData.Position;
            transform.rotation = recData.Rotation;
        }
        bool IsMouseOverGameWindow { get { return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y); } }
        private void InputMove(out MoveDataPlayer moveData)
        {
            moveData = default;
            if (!fetchInput) return;
            if (Input.mousePresent && IsMouseOverGameWindow)
            {
                if (Input.GetMouseButton(0))
                    _primaryFire = true;

                if (Input.GetMouseButton(1))
                    _secondaryFire = true;

                moveData.MoveDelta = Vector2.zero;

                if (Input.GetKey(KeyCode.W))
                    moveData.MoveDelta += Vector2.up;

                if (Input.GetKey(KeyCode.S))
                    moveData.MoveDelta += Vector2.down;

                if (Input.GetKey(KeyCode.A))
                    moveData.MoveDelta += Vector2.left;

                if (Input.GetKey(KeyCode.D))
                    moveData.MoveDelta += Vector2.right;

                Vector3 mousePos = Input.mousePosition;

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(mousePos);

                Vector3 mouseCollisionPoint = Vector3.zero;
                // Raycast towards the mouse collider box in the world
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, _mouseRayMask))
                {
                    if (hit.collider != null)
                    {
                        mouseCollisionPoint = hit.point;
                    }
                }

                Vector3 aimDirection = mouseCollisionPoint - _player.turretPosition;
                moveData.AimDelta = new Vector2(aimDirection.x, aimDirection.z);
            }
          
        }
        
        [Client( RequireOwnership = true)]
        private void UpdateFire()
        {
            if (Input.GetMouseButton(0))
                _player.shooter.OnFireWeapon((WeaponManager.WeaponInstallationType.PRIMARY));
            
            if (Input.GetMouseButton(1))
                _player.shooter.OnFireWeapon((WeaponManager.WeaponInstallationType.SECONDARY));
        }
        
    }
   
}