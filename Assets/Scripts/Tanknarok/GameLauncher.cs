using Fusion;
using FishNetworking.FishnetHelpers;
using FishNetworking.UIHelpers;
using Tanknarok.UI;
using TMPro;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;

namespace FishNetworking.Tanknarok
{
    /// <summary>
    /// App entry point and main UI flow management.
    /// </summary>
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManagerPrefab;
        [SerializeField] private Player _playerPrefab;
        [SerializeField] private TMP_InputField _room;
        [SerializeField] private TextMeshProUGUI _progress;
        [SerializeField] private Panel _uiCurtain;
        [SerializeField] private Panel _uiStart;
        [SerializeField] private Panel _uiProgress;
        [SerializeField] private Panel _uiRoom;
        [SerializeField] private GameObject _uiGame;
        private NetworkManager _networkManager;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private FishnetLauncher.ConnectionStatus _status = FishnetLauncher.ConnectionStatus.Disconnected;
        private GameMode _gameMode;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager not found, HUD will not function.");
                return;
            }
            else
            {
                _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
                _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            }
            OnConnectionStatusUpdate(null, LocalConnectionState.Stopped, "");
        }

        private void Update()
        {
            if (_uiProgress.isShowing)
            {
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    NetworkRunner runner = FindObjectOfType<NetworkRunner>();
                    if (runner != null && !runner.IsShutdown)
                    {
                        // Calling with destroyGameObject false because we do this in the OnShutdown callback on FishnetLauncher
                        runner.Shutdown(false);
                    }
                }
                UpdateUI();
            }
        }

        // What mode to play - Called from the start menu
        public void OnHostOptions()
        {
            SetGameMode(GameMode.Host);
        }

        public void OnJoinOptions()
        {
            SetGameMode(GameMode.Client);
        }

        public void OnSharedOptions()
        {
            SetGameMode(GameMode.Shared);
        }

        private void SetGameMode(GameMode gamemode)
        {
            _gameMode = gamemode;
            if (GateUI(_uiStart))
                _uiRoom.SetVisible(true);
        }

        public void OnEnterRoom()
        {
            if (GateUI(_uiRoom))
            {
                FishnetLauncher launcher = FindObjectOfType<FishnetLauncher>();
                if (launcher == null)
                    launcher = new GameObject("Launcher").AddComponent<FishnetLauncher>();

                LevelManager lm = FindObjectOfType<LevelManager>();
                lm.launcher = launcher;

                // launcher.Launch(_gameMode, _room.text, lm, OnConnectionStatusUpdate, OnSpawnWorld, OnSpawnPlayer, OnDespawnPlayer);
            }
        }

        /// <summary>
        /// Call this method from button events to close the current UI panel and check the return value to decide
        /// if it's ok to proceed with handling the button events. Prevents double-actions and makes sure UI panels are closed. 
        /// </summary>
        /// <param name="ui">Currently visible UI that should be closed</param>
        /// <returns>True if UI is in fact visible and action should proceed</returns>
        private bool GateUI(Panel ui)
        {
            if (!ui.isShowing)
                return false;
            ui.SetVisible(false);
            return true;
        }

        private void OnConnectionStatusUpdate(NetworkRunner runner, LocalConnectionState status, string reason)
        {
            if (!this)
                return;

            Debug.Log(status);

            if (status != _clientState)
            {
                switch (status)
                {
                    case LocalConnectionState.Stopped:
                        ErrorBox.Show("Disconnected!", reason, () => { });
                        break;
                }
            }

            _clientState = status;
            UpdateUI();
        }

        private void OnSpawnWorld(NetworkRunner runner)
        {
            Debug.Log("Spawning GameManager");
            runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity, null, InitNetworkState);
            void InitNetworkState(NetworkRunner runner, NetworkObject world)
            {
                world.transform.parent = transform;
            }
        }

        private void OnSpawnPlayer(NetworkRunner runner, PlayerRef playerref)
        {
            if (GameManager.playState != GameManager.PlayState.LOBBY)
            {
                Debug.Log("Not Spawning Player - game has already started");
                return;
            }
            Debug.Log($"Spawning tank for player {playerref}");
            runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, playerref, InitNetworkState);
            void InitNetworkState(NetworkRunner runner, NetworkObject networkObject)
            {
                Player player = networkObject.gameObject.GetComponent<Player>();
                Debug.Log($"Initializing player {player.playerID}");
                player.InitNetworkState(GameManager.MAX_LIVES);
            }
        }

        private void OnDespawnPlayer(NetworkRunner runner, PlayerRef playerref)
        {
            Debug.Log($"Despawning Player {playerref}");
            Player player = PlayerManager.Get(playerref);
            player.TriggerDespawn();
        }

        private void UpdateUI()
        {
            bool intro = false;
            bool progress = false;
            bool running = false;
            Debug.Log(_clientState);
            switch (_status)
            {
                // case FishnetLauncher.ConnectionStatus.Disconnected:
                //     _progress.text = "Disconnected!";
                //     intro = true;
                //     break;
                // case FishnetLauncher.ConnectionStatus.Failed:
                //     _progress.text = "Failed!";
                //     intro = true;
                //     break;
                // case FishnetLauncher.ConnectionStatus.Connecting:
                //     _progress.text = "Connecting";
                //     progress = true;
                //     break;
                // case FishnetLauncher.ConnectionStatus.Connected:
                //     _progress.text = "Connected";
                //     progress = true;
                //     break;
                // case FishnetLauncher.ConnectionStatus.Loading:
                //     _progress.text = "Loading";
                //     progress = true;
                //     break;
                // case FishnetLauncher.ConnectionStatus.Loaded:
                //     running = true;
                //     break;
            }
            switch (_clientState)
            {
                case LocalConnectionState.Stopped:
                    _progress.text = "Disconnected!";
                    intro = true;
                    progress = false;
                    break;
                case LocalConnectionState.Starting:
                    _progress.text = "Connecting";
                    intro = false;
                    progress = true;
                    break;
                case LocalConnectionState.Started:
                    _progress.text = "Connected";
                    running = true;
                    progress = false;
                    break;
                case LocalConnectionState.Stopping:
                    _progress.text = "Disconnecting";
                    progress = true;
                    break;
            }
            _uiCurtain.SetVisible(!running);
            _uiStart.SetVisible(intro);
            _uiProgress.SetVisible(progress);
            _uiGame.SetActive(running);

            if (intro)
                MusicPlayer.instance.SetLowPassTranstionDirection(-1f);
        }
        private void OnDestroy()
        {
            if (_networkManager == null)
                return;

            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            _clientState = obj.ConnectionState;
        }


        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            _serverState = obj.ConnectionState;
        }


        public void OnClick_Server()
        {
            if (_networkManager == null)
                return;

            if (_serverState != LocalConnectionState.Stopped)
            {
                _networkManager.ServerManager.StopConnection(true);
                // if (GateUI(_uiStart))
                //     _uiRoom.SetVisible(true);
            }
            else
            {
                _networkManager.ServerManager.StartConnection();
                // if (GateUI(_uiStart))
                //     _uiRoom.SetVisible(true);
            }
            OnClick_Client();
        }


        public void OnClick_Client()
        {
            if (_networkManager == null)
                return;

            if (_clientState != LocalConnectionState.Stopped)
            {
                _networkManager.ClientManager.StopConnection();
                GateUI(_uiStart);
            }
            else
            {
                _networkManager.ClientManager.StartConnection();
                GateUI(_uiStart);
            }

        }
        string GetNextStateText(LocalConnectionState state)
        {
            if (state == LocalConnectionState.Stopped)
                return "Start";
            else if (state == LocalConnectionState.Starting)
                return "Starting";
            else if (state == LocalConnectionState.Stopping)
                return "Stopping";
            else if (state == LocalConnectionState.Started)
                return "Stop";
            else
                return "Invalid";
        }
    }
}