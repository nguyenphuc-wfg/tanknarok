using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FishNetworking.Tanknarok
{
    public class SceneGameManager: MonoBehaviour
    {
        [SerializeField] private CameraScreenFXBehaviour _transitionEffect;
        [SerializeField] private ScoreManager _scoreManager;
        public void SwitchSceneRound()
        {
            StartCoroutine(SwithScene());
        }
        private IEnumerator SwithScene()
        {
            InputController.fetchInput = false;

            // Despawn players with a small delay between each one
            Debug.Log("De-spawning all tanks");
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Debug.Log($"De-spawning tank {i}:{PlayerManager.allPlayers[i]}");
                PlayerManager.allPlayers[i].DespawnTank();
                yield return new WaitForSeconds(1f);
            }
            _scoreManager.HideUiScoreAndReset(false);
            _transitionEffect.ToggleGlitch(true);
            
            for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
            {
                Player player = PlayerManager.allPlayers[i];
                Debug.Log($"Respawning Player {i}:{player}");
                player.ResetState(3);
                player.Respawn(0);
                yield return new WaitForSeconds(0.3f);
            }
            InputController.fetchInput = true;
            _transitionEffect.ToggleGlitch(false);
        }
    }
}