using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNetworking.UIHelpers;
using TMPro;
using UnityEngine.SceneManagement;

namespace FishNetworking.Tanknarok
{
    public class GameLauncher : MonoBehaviour
    {
        // [SerializeField] private GameManager _gameManagerPrefab;
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
            UpdateUI();
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
            UpdateUI();
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
                _networkManager.ServerManager.StopConnection(true);
            else
                _networkManager.ServerManager.StartConnection();
            OnClick_Client();
        }


        public void OnClick_Client()
        {
            if (_networkManager == null)
                return;

            if (_clientState != LocalConnectionState.Stopped)
                _networkManager.ClientManager.StopConnection();
            else
                _networkManager.ClientManager.StartConnection();
        }
        private void UpdateUI()
        {
            bool intro = false;
            bool progress = false;
            bool running = false;

            switch (_clientState)
            {
                case LocalConnectionState.Stopped:
                    _progress.text = "Disconnected!";
                    intro = true;
                    break;
                case LocalConnectionState.Starting:
                    _progress.text = "Connecting";
                    intro = false;
                    break;
                case LocalConnectionState.Started:
                    _progress.text = "Connected";
                    running = true;
                    progress = false;
                    break;
                case LocalConnectionState.Stopping:
                    _progress.text = "Disconnecting";
                    intro = false;
                    progress = true;
                    SceneManager.UnloadSceneAsync(1);
                    break;
            }

            _uiCurtain.SetVisible(!running);
            _uiStart.SetVisible(intro);
            _uiProgress.SetVisible(progress);
            if (_uiGame)
                _uiGame.SetActive(running);
            if (running) SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        }
    }

}
