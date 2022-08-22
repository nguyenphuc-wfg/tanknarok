using System;
using FishNet.Managing.Logging;
using UnityEngine;
using FishNet.Object;

namespace FishNetworking.Tanknarok
{
    public class RotatingTurret : NetworkBehaviour
    {
        [SerializeField] private LaserBeam[] _laserBeams;
        [SerializeField] private float _rpm;

        private float _rotationSpeed;

        private void Start()
        {
            RpcInit();
        }

        private void RpcInit()
        {
            for (int i = 0; i < _laserBeams.Length; i++)
                _laserBeams[i].Init();
        }

       // [Server(Logging = LoggingType.Off)]
        // Rotates the turret and updates laser beams
        public void FixedUpdate()
        {
            if(IsServer)
                transform.Rotate(0, _rpm * Time.deltaTime, 0);
            RpcRotateInfinity();
        }

        private void RpcRotateInfinity()
        {
            for (int i = 0; i < _laserBeams.Length; i++)
            {
                _laserBeams[i].UpdateLaserBeam();
            }
        }
    }
}