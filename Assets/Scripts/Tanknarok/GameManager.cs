using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet;
namespace FishNetworking.Tanknarok
{
    public class GameManager : NetworkBehaviour
    {
        public enum PlayState
        {
            LOBBY,
            LEVEL,
            TRANSITION
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("game manager iss he2222");
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("game manager client iss he2222");
        }
        // public static PlayState playState
        // {
        //     get => (instance != null && instance.Object != null && instance.Object.IsValid) ? instance.networkedPlayState : PlayState.LOBBY;
        //     set
        //     {
        //         if (instance != null && instance.Object != null && instance.Object.IsValid)
        //             instance.networkedPlayState = value;
        //     }
        // }

        // public static int WinningPlayerIndex
        // {
        //     get => (instance != null && instance.Object != null && instance.Object.IsValid) ? instance.networkedWinningPlayerIndex : -1;
        //     set
        //     {
        //         if (instance != null && instance.Object != null && instance.Object.IsValid)
        //             instance.networkedWinningPlayerIndex = value;
        //     }
        // }
        public const byte MAX_LIVES = 3;
        public const byte MAX_SCORE = 3;
        private LevelManager _levelManager;
        private bool _restart = true;
        public static GameManager instance { get; private set; }
        public void Restart()
        {
            {
                // Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
                instance = null;
                _restart = false;
            }
        }
        // public const ShutdownReason ShutdownReason_GameAlreadyRunning = (ShutdownReason)100;
        private void Update()
        {
            if (_restart || Input.GetKeyDown(KeyCode.Escape))
            {
                _restart = false;
                Restart();
                return;
            }
            PlayerManager.HandleNewPlayers();
        }
    }

}
