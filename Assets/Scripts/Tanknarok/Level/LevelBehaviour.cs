using System;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public class LevelBehaviour : MonoBehaviour
    {
        // Class for storing the lighting settings of a level
        [System.Serializable]
        public struct LevelLighting
        {
            public Color ambientColor;
            public Color fogColor;
            public bool fog;
        }

        [SerializeField] private LevelLighting _levelLighting;

        [SerializeField] private SpawnPoint[] _playerSpawnPoints;
        private LevelManager _levelManager;
        private void Awake()
        {
            _playerSpawnPoints = GetComponentsInChildren<SpawnPoint>(true);

        }

        private void Start()
        {
            _levelManager = FindObjectOfType<LevelManager>(true);
            if (_levelManager != null)
                _levelManager.SetNewLevelBehaviour(this);

        }

        public void Activate()
        {
            SetLevelLighting();
        }

        private void SetLevelLighting()
        {
            RenderSettings.ambientLight = _levelLighting.ambientColor;
            RenderSettings.fogColor = _levelLighting.fogColor;
            RenderSettings.fog = _levelLighting.fog;
        }

        public SpawnPoint GetPlayerSpawnPoint(int id)
        {
            return _playerSpawnPoints[id].GetComponent<SpawnPoint>();
        }
    }
}