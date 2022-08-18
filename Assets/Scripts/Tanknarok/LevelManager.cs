using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace FishNetworking.Tanknarok
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private int _lobby;
        [SerializeField] private int[] _levels;
        [SerializeField] private LevelBehaviour _currentLevel;

        private Scene _loadedScene;
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
        // public void LoadLevel(int nextLevelIndex)
        // {
        //     Runner.SetActiveScene(nextLevelIndex < 0 ? _lobby : _levels[nextLevelIndex]);
        // }
    }

}
