using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    /// <summary>
    /// Handle player input by responding to Fusion input polling, filling an input struct and then working with
    /// that input struct in the Fusion Simulation loop.
    /// </summary>
    public class InputController : NetworkBehaviour
    {
        [SerializeField] private LayerMask _mouseRayMask;

        public static bool fetchInput = true;
        public bool ToggleReady { get; set; }

        [SerializeField] private Player _player;
        private NetworkInputData _frameworkInput = new NetworkInputData();
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
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    _player.ToggleReady();
                }
            }
            if (!fetchInput) return;
            FixedUpdateFire();
            Move();
        }

        private void Move()
        {
            if (Input.mousePresent)
            {
                if (Input.GetMouseButton(0))
                    _primaryFire = true;

                if (Input.GetMouseButton(1))
                    _secondaryFire = true;

                _moveDelta = Vector2.zero;

                if (Input.GetKey(KeyCode.W))
                    _moveDelta += Vector2.up;

                if (Input.GetKey(KeyCode.S))
                    _moveDelta += Vector2.down;

                if (Input.GetKey(KeyCode.A))
                    _moveDelta += Vector2.left;

                if (Input.GetKey(KeyCode.D))
                    _moveDelta += Vector2.right;

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
                _aimDelta = new Vector2(aimDirection.x, aimDirection.z);
                _player.SetDirections(_moveDelta.normalized, _aimDelta.normalized);
                _player.Move();

            }
            else if (Input.touchSupported)
            {
                bool leftIsDown = false;
                bool rightIsDown = false;

                foreach (Touch touch in Input.touches)
                {
                    if (touch.position.x < Screen.width / 2)
                    {
                        leftIsDown = true;
                        _leftPos = touch.position;
                        if (_leftTouchWasDown)
                            _moveDelta += 10.0f * touch.deltaPosition / Screen.dpi;
                        else
                            _leftDown = touch.position;
                    }
                    else
                    {
                        rightIsDown = true;
                        _rightPos = touch.position;
                        if (_rightTouchWasDown && (touch.position - _rightDown).magnitude > (0.01f * Screen.dpi))
                            _aimDelta = (10.0f / Screen.dpi) * (touch.position - _rightDown);
                        else
                            _rightDown = touch.position;
                    }
                }
                if (_rightTouchWasDown && !rightIsDown)
                    _primaryFire = true;
                if (_leftTouchWasDown && !leftIsDown && _moveDelta.magnitude < 0.01f)
                    _secondaryFire = true;

                if (!leftIsDown)
                    _moveDelta = Vector2.zero;

                // _mobileInput.gameObject.SetActive(true);
                // _mobileInput.SetLeft(leftIsDown, _leftDown, _leftPos);
                // _mobileInput.SetRight(rightIsDown, _rightDown, _rightPos);

                _leftTouchWasDown = leftIsDown;
                _rightTouchWasDown = rightIsDown;
            }
            else
            {
                // _mobileInput.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// FixedUpdateNetwork is the main Fusion simulation callback - this is where
        /// we modify network state.
        /// </summary>
        private void FixedUpdateFire()
        {
            if (!base.IsOwner)
                return;
            // if (GameManager.playState == GameManager.PlayState.TRANSITION)
            //     return;
            // Get our input struct and act accordingly. This method will only return data if we
            // have Input or State Authority - meaning on the controlling player or the server.
            Vector2 direction = default;
            // if (Input.GetMouseButtonDown(0))
            // {
            //     direction = _aimDelta.normalized;
            //
            //     if (input.IsDown(NetworkInputData.BUTTON_FIRE_PRIMARY))
            //     {
            //         _player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.PRIMARY);
            //     }
            //
            //     if (input.IsDown(NetworkInputData.BUTTON_FIRE_SECONDARY))
            //     {
            //         _player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.SECONDARY);
            //     }
            //
            //     // if (input.IsDown(NetworkInputData.READY))
            //     // {
            //     //     _player.ToggleReady();
            //     // }
            //
            //     _player.SetDirections(direction, input.aimDirection.normalized);
            // }
            if (Input.GetMouseButton(0))
            {
                direction = _aimDelta.normalized;
                _player.SetDirections(_moveDelta.normalized, _aimDelta.normalized);
                _player.shooter.FireWeapon((WeaponManager.WeaponInstallationType.PRIMARY));
            }
            if (Input.GetMouseButton(1))
            {
                direction = _aimDelta.normalized;
                _player.SetDirections(_moveDelta.normalized, _aimDelta.normalized);
                _player.shooter.FireWeapon((WeaponManager.WeaponInstallationType.SECONDARY));
            }

        }
        
    }

    /// <summary>
    /// Our custom definition of an INetworkStruct. Keep in mind that
    /// * bool does not work (C# does not define a consistent size on different platforms)
    /// * Must be a top-level struct (cannot be a nested class)
    /// * Stick to primitive types and structs
    /// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
    /// </summary>
    public struct NetworkInputData
    {
        public const uint BUTTON_FIRE_PRIMARY = 1 << 0;
        public const uint BUTTON_FIRE_SECONDARY = 1 << 1;
        public const uint READY = 1 << 6;

        public uint Buttons;
        public Vector2 aimDirection;
        public Vector2 moveDirection;

        public bool IsUp(uint button)
        {
            return IsDown(button) == false;
        }

        public bool IsDown(uint button)
        {
            return (Buttons & button) == button;
        }
    }
}