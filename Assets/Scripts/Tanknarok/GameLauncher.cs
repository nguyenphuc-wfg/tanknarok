using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Managing;
using FishNet.Transporting;
using FishNetworking.UIHelpers;
using FishNet.Connection;
using TMPro;
using UnityEngine.SceneManagement;
namespace FishNetworking.Tanknarok
{
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private NetworkObject _gameManagerPrefab;
        // [SerializeField] private Player _playerPrefab;
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
            UpdateUI(LocalConnectionState.Stopped);
        }
        private void Update()
        {
            if (_uiGame.activeSelf)
            {
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    _networkManager.ServerManager.StopConnection(true);
                    _networkManager.ClientManager.StopConnection();
                }
            }

        }
        private void OnSpawnWorld()
        {
            if (_networkManager == null)
                return;
            NetworkObject go = Instantiate(_gameManagerPrefab, this.transform);
            _networkManager.ServerManager.Spawn(go);
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
            UpdateUI(_clientState);
        }


        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            _serverState = obj.ConnectionState;
            UpdateUI(_serverState);
        }


        public void OnClick_Server()
        {
            if (_networkManager == null)
                return;

            if (_serverState != LocalConnectionState.Stopped)
                _networkManager.ServerManager.StopConnection(true);
            else {
                _networkManager.ServerManager.StartConnection();    
                OnSpawnWorld();
            }
                
        }
    

        public void OnClick_Client()
        {
            if (_networkManager == null)
                return;
            if (_serverState != LocalConnectionState.Stopped) 
                return;
                
            if (_clientState != LocalConnectionState.Stopped)
                _networkManager.ClientManager.StopConnection();
            else {
                _networkManager.ClientManager.StartConnection();
                OnSpawnWorld();
            }
                
        }
        private void UpdateUI(LocalConnectionState state)
        {
            bool intro = false;
            bool progress = false;
            bool running = false;

            switch (state)
            {
                case LocalConnectionState.Stopped:
                    _progress.text = "";
                    intro = true;
                    break;
                case LocalConnectionState.Starting:
                    _progress.text = "Connecting";
                    break;
                case LocalConnectionState.Started:
                    _progress.text = "Connected";
                    running = true;
                    break;
                case LocalConnectionState.Stopping:
                    _progress.text = "Disconnecting";
                    progress = true;
                    SceneManager.UnloadSceneAsync(2);
                    break;
            }

            _uiCurtain.SetVisible(!running);
            _uiStart.SetVisible(intro);
            _uiProgress.SetVisible(progress);
            if (_uiGame)
                _uiGame.SetActive(running);
            if (running) SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        }
    }

}
