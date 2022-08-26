using Cysharp.Threading.Tasks;
using UnityEngine;
using FishNet.Object;
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
        private int networkedWinningPlayerIndex = -1;
        private PlayState networkedPlayState;
        public static PlayState playState;
        
        public const byte MAX_LIVES = 2;
        public const byte MAX_SCORE = 2;
        private ScoreManager _scoreManager;
        private LevelManager _levelManager;
        private bool _restart = true;
        public static GameManager instance { get; private set; }

        private void Awake()
        {
            if (instance == null)
                instance = this;
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
            _restart = false;
        }
        private void Update()
        {
            if (_restart || Input.GetKeyDown(KeyCode.Escape))
            {
                Restart();
                return;
            }
            PlayerManager.HandleNewPlayers();
        }
        
        public void OnTankDeath()
        {
            if (playState == PlayState.LOBBY) return;
            
            int playersleft = PlayerManager.PlayersAlive();
            Debug.Log($"Someone died - {playersleft} left");
            
            if (playersleft > 1) return;
            
            Player lastPlayerStanding = playersleft == 0 ? null : PlayerManager.GetFirstAlivePlayer();

            if (lastPlayerStanding == null) return;
            
            int winningPlayerIndex = lastPlayerStanding.playerID;
            byte winningPlayerScore = (byte)(lastPlayerStanding.score + 1);
            
            if (winningPlayerIndex < 0) return;
            
            lastPlayerStanding.score = winningPlayerScore;
            if (winningPlayerScore >= MAX_SCORE)
            {
                _levelManager.OnScoreLobby(winningPlayerIndex, winningPlayerScore);
                return;
            }
            _levelManager.OnScoreShow(winningPlayerIndex, winningPlayerScore);
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
