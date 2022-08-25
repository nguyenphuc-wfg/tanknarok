using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FishNet.Object;
using FishNet;
using FishNet.Object.Synchronizing;
using FishNetworking.Event;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("tankPlayerEvent")] public GameEvent tankDeathEvent;
        private int networkedWinningPlayerIndex = -1;
        private PlayState networkedPlayState;
        public static PlayState playState;
        
        public const byte MAX_LIVES = 2;
        public const byte MAX_SCORE = 2;
        private ScoreManager _scoreManager;
        private LevelManager _levelManager;
        private bool _restart = true;
        public static GameManager instance { get; private set; }

        public void OnTankDeath()
        {
            if (playState != PlayState.LOBBY)
            {
                int playersleft = PlayerManager.PlayersAlive();
                Debug.Log($"Someone died - {playersleft} left");
                if (playersleft<=1)
                {
                    Player lastPlayerStanding = playersleft == 0 ? null : PlayerManager.GetFirstAlivePlayer();
                    // if there is only one player, who died from a laser (e.g.) we don't award scores. 
                    if (lastPlayerStanding != null)
                    {
                        int winningPlayerIndex = lastPlayerStanding.playerID;
                        byte winningPlayerScore = (byte)(lastPlayerStanding.score + 1);
                        if (winningPlayerIndex >= 0)
                        {
                            lastPlayerStanding.score = winningPlayerScore;
                            if (winningPlayerScore >= MAX_SCORE)
                            {
                                _levelManager.OnScoreLobby(winningPlayerIndex, winningPlayerScore);
                                return;
                            }
                            _levelManager.OnScoreShow(winningPlayerIndex, winningPlayerScore);
                        }
                        
                    }
                }
            }
        }


        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                InstanceFinder.ServerManager.Despawn(this.NetworkObject);
            _scoreManager = FindObjectOfType<ScoreManager>(true);
            _levelManager = FindObjectOfType<LevelManager>(true);
        }

        private void Start()
        {
            playState = PlayState.LOBBY;
        }

        private void OnEnable()
        {
            tankDeathEvent.Sub(OnTankDeath);
        }
        private void OnDisable()
        {
            tankDeathEvent.UnSub(OnTankDeath);
        }
        public void Restart()
        {
            // Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
            // instance = null;
            _restart = false;
        }
        // public const ShutdownReason ShutdownReason_GameAlreadyRunning = (ShutdownReason)100;
        private void Update()
        {
            if (_restart || Input.GetKeyDown(KeyCode.Escape))
            {
                Restart();
                return;
            }
            PlayerManager.HandleNewPlayers();
        }
        

        // Transition from lobby to level
        public async UniTask OnAllPlayersReady()
        {
            Debug.Log("All players are ready");
            await UniTask.Delay(1000);
            if (playState!=PlayState.LOBBY)
                return;
            
            _levelManager.ReadyToStartMatch();
        }
		
        
        
    }

}
