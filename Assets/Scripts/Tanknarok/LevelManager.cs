using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace FishNetworking.Tanknarok
{
    public class LevelManager : NetworkBehaviour
    {
        [SerializeField] private int _lobby;
        [SerializeField] private int[] _levels;
        [SerializeField] private LevelBehaviour _currentLevel;

        private Scene _loadedScene;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private ReadyupManager _readyupManager;
        [SerializeField] private CountdownManager _countdownManager;
        [SerializeField] private SceneGameManager _sceneManager;

        private void Awake()
        {
            _countdownManager.Reset();
            _scoreManager.HideLobbyScore();
            _readyupManager.HideUI();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _scoreManager.HideUiScoreAndReset(true);
        }

        public int GetRandomLevelIndex()
        {
            int idx = Random.Range(0, _levels.Length);
            // Make sure it's not the same level again. This is partially because it's more fun to try different levels and partially because scene handling breaks if trying to load the same scene again.
            if (_levels[idx] == _loadedScene.buildIndex)
                idx = (idx + 1) % _levels.Length;
            return idx;
        }
        public SpawnPoint GetPlayerSpawnPoint(int playerID)
        {
            if (_currentLevel != null)
                return _currentLevel.GetPlayerSpawnPoint(playerID);
            return null;
        }
        
        public void SetNewLevelBehaviour(LevelBehaviour lvl)
        {
            _currentLevel = lvl;
            if(_currentLevel!=null)
                _currentLevel.Activate();
        }

        [ObserversRpc(RunLocally = true)]
        public void OnScoreShow(int playerId, byte score)
        {
            _scoreManager.UpdateScore(playerId, score);
            _sceneManager.SwitchSceneRound();
        }
        [ObserversRpc(RunLocally = true)]
        public void OnScoreLobby(int playerId, byte score)
        {
            _scoreManager.UpdateScore(playerId, score);
            _sceneManager.OnEndMath(playerId, score,  _readyupManager.ShowUI);
        }
        
        [ServerRpc(RunLocally = true)]
        public void ReadyToStartMatch()
        {
            ResetStats();
            ResetLives();
            LoadLevel();
        }
        
        private void ResetStats()
        {
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"Resetting player {i} stats to lives={GameManager.MAX_LIVES}");
                PlayerManager.allPlayers[i].lives = GameManager.MAX_LIVES;
                PlayerManager.allPlayers[i].score = 0;
            }
        }

        private void ResetLives()
        {
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"Resetting player {i} lives to {GameManager.MAX_LIVES}");
                PlayerManager.allPlayers[i].lives = GameManager.MAX_LIVES;
            }
        }
        public void LoadLevel()
        {
            // Reset players ready state so we don't launch immediately
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                PlayerManager.allPlayers[i].ResetReady();
                PlayerManager.allPlayers[i].DespawnTank();
            }
            

            GameManager.playState = GameManager.PlayState.LEVEL;
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                PlayerManager.allPlayers[i].RespawnPlay();
            }
            OnStartMatch();
            _countdownManager.Countdown(_sceneManager.OnStartedMatch);
        }
        
        [ObserversRpc(RunLocally = true)]
        public void OnStartMatch()
        {
            _readyupManager.HideUI();
            _sceneManager.OnStartMatch();
        }
    }

}
