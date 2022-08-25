using System;
using UnityEngine;
using FishNet.Object;
using FishNet;
using FishNet.Object.Synchronizing;
using FishNetworking.Event;
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
        [SyncVar]
        private int networkedWinningPlayerIndex = -1;

        [SyncVar] private PlayState networkedPlayState;
        public static PlayState playState
        {
            get => (instance != null && instance.NetworkObject != null) ? instance.networkedPlayState : PlayState.LOBBY;
            set
            {
                if (instance != null && instance.NetworkObject != null)
                    instance.networkedPlayState = value;
            }
        }

        public static int WinningPlayerIndex
        {
            get => (instance != null && instance.NetworkObject != null) ? instance.networkedWinningPlayerIndex : -1;
            set
            {
                if (instance != null && instance.NetworkObject != null)
                    instance.networkedWinningPlayerIndex = value;
            }
        }
        
        public const byte MAX_LIVES = 3;
        public const byte MAX_SCORE = 3;
        private ScoreManager _scoreManager;
        private LevelManager _levelManager;
        private bool _restart = true;
        public static GameManager instance { get; private set; }

        public void OnTankDeath()
        {
            // if (playState != PlayState.LOBBY)
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
            _scoreManager = FindObjectOfType<ScoreManager>(true);
            _levelManager = FindObjectOfType<LevelManager>(true);
        }

        private void Start()
        {
            playState = PlayState.LEVEL;
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
            instance = null;
            _restart = false;
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
        private void ResetStats()
        {
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"Resetting player {i} stats to lives={MAX_LIVES}");
                PlayerManager.allPlayers[i].lives = MAX_LIVES;
                PlayerManager.allPlayers[i].score = 0;
            }
        }

        private void ResetLives()
        {
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"Resetting player {i} lives to {MAX_LIVES}");
                PlayerManager.allPlayers[i].lives = MAX_LIVES;
            }
        }

        // Transition from lobby to level
        public void OnAllPlayersReady()
        {
            Debug.Log("All players are ready");
            if (playState!=PlayState.LOBBY)
                return;

            // Reset stats and transition to level.
            ResetStats();

            LoadLevel(_levelManager.GetRandomLevelIndex(),-1);
        }
		
        private void LoadLevel(int nextLevelIndex, int winningPlayerIndex)
        {
            // if (!Object.HasStateAuthority)
            //     return;

            // Reset lives and transition to level
            ResetLives();

            // Reset players ready state so we don't launch immediately
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
                PlayerManager.allPlayers[i].ResetReady();

            // Start transition
            WinningPlayerIndex = winningPlayerIndex;

            _levelManager.LoadLevel();
        }

        public void StateAuthorityChanged()
        {
            // Debug.Log($"State Authority of GameManager changed: {Object.StateAuthority}");
        }
    }

}
