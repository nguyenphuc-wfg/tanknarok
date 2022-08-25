using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FishNetworking.Tanknarok
{
    public class SceneGameManager: MonoBehaviour
    {
        [SerializeField] private CameraScreenFXBehaviour _transitionEffect;
        [SerializeField] private ScoreManager _scoreManager;
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
                player.ResetState(3);
                player.Respawn(0);
                await UniTask.Delay(300);
            }
            InputController.fetchInput = true;
            _transitionEffect.ToggleGlitch(false);
        }

        public void OnEndMath(int playerId, byte score)
        {
            EndMatch(playerId, score);
        }
        private async UniTask EndMatch(int playerId, byte score)
        {
            InputController.fetchInput = false;

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
                player.ResetState(3);
                player.Respawn(0);
                await UniTask.Delay(300);
            }
            _transitionEffect.ToggleGlitch(false);
            await UniTask.Delay(100);
            _scoreManager.ShowLobbyScore(playerId, score);
        }
    }
}