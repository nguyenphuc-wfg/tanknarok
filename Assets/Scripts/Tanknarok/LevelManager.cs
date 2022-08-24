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
        public void LoadLevel()
        {
        }

        [ObserversRpc(RunLocally = true)]
        public void OnScoreShow(int playerId, byte score)
        {
            _scoreManager.UpdateScore(playerId, score);
            _sceneManager.SwitchSceneRound();
        }
        [ObserversRpc(RunLocally = true)]
        public void OnScoreLobby(int playerId)
        {
            _scoreManager.ShowLobbyScore(playerId);
        }
        
    }

}
