using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNetworking.UIHelpers;
using UnityEngine.SceneManagement;

namespace FishNetworking.Tanknarok
{
    public class SceneGameManager: MonoBehaviour
    {
        private static int _sceneIdPlay;
        [SerializeField] private CameraScreenFXBehaviour _transitionEffect;
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private GameObject _uiGame;
        public void OnStartMatch()
        {
            _scoreManager.HideLobbyScore();
            InputController.fetchInput = false;
        }

        public void OnStartedMatch()
        {
            InputController.fetchInput = true;
            _uiGame.SetActive(false);
        }
        public void SwitchSceneRound()
        {
            SwithScene();
        }
        private async UniTask SwithScene()
        {
            InputController.fetchInput = false;
            await UniTask.Delay(300);
            // Despawn players with a small delay between each one
            Debug.Log("De-spawning all tanks");
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"De-spawning tank {i}:{PlayerManager.allPlayers[i]}");
                PlayerManager.allPlayers[i].DespawnTank();
                await UniTask.Delay(1000);
            }
            _scoreManager.HideUiScoreAndReset(false);
            _transitionEffect.ToggleGlitch(true);
            
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Player player = PlayerManager.allPlayers[i];
                Debug.Log($"Respawning Player {i}:{player}");
                player.ResetState(GameManager.MAX_LIVES);
                player.Respawn(0);
                await UniTask.Delay(300);
            }
            InputController.fetchInput = true;
            _transitionEffect.ToggleGlitch(false);
        }

        public void OnEndMath(int playerId, byte score, Action callBack)
        {
            EndMatch(playerId, score, callBack);
        }
        private async UniTask EndMatch(int playerId, byte score, Action callBack)
        {
            InputController.fetchInput = false;
            GameManager.playState = GameManager.PlayState.LOBBY;
            // Despawn players with a small delay between each one
            Debug.Log("De-spawning all tanks");
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"De-spawning tank {i}:{PlayerManager.allPlayers[i]}");
                PlayerManager.allPlayers[i].DespawnTank();
                await UniTask.Delay(1000);
            }
            
            _scoreManager.HideUiScoreAndReset(true);
            _transitionEffect.ToggleGlitch(true);
            
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Player player = PlayerManager.allPlayers[i];
                Debug.Log($"Respawning Player {i}:{player}");
                player.InitNetworkState(GameManager.MAX_LIVES);
                player.Respawn(0);
                await UniTask.Delay(300);
            }
            _transitionEffect.ToggleGlitch(false);
            await UniTask.Delay(100);
            _scoreManager.ShowLobbyScore(playerId, score);
            InputController.fetchInput = true;
            _uiGame.SetActive(true);
            callBack?.Invoke();
        }

        public static void LoadSceneGame(int sceneId, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadSceneAsync(sceneId, mode);
            _sceneIdPlay = sceneId;
        }

        public static void UnLoadSceneGame(int sceneId)
        {
            SceneManager.UnloadSceneAsync(sceneId);
        }

        public static void UnLoadSceneGameDisconnect()
        {
            SceneManager.UnloadSceneAsync(_sceneIdPlay);
        }
    }
}