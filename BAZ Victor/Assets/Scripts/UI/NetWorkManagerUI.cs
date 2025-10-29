using System;
using Enum;
using EventBus;
using Manager;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    
    public class NetworkManagerUI : MonoBehaviour 
    {
        #region Fields

        [Header("Panels")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject launchGamePanel;
        [SerializeField] private GameObject mainBackground;
        
        [Header("Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitButtons();
        }

        private void Update()
        {
            LaunchGame();
        }

        #endregion

        #region Initialization

        private void InitButtons()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            
            if (clientButton != null) clientButton.onClick.AddListener(OnClientClicked);
            
        }

        #endregion
        
        #region Button Callbacks

        private void OnHostClicked()
        {
            NetworkManager.Singleton.StartHost();
        }

        private void OnClientClicked()
        {
            NetworkManager.Singleton.StartClient();
        }

        private void OnStartGameClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LaunchGame();
            }
        }

        #endregion
        
        #region Event Listeners

        private void OnEnable()
        {
            EventManager.OnMenu += OnMenuState;
            EventManager.OnLobbyEntered += OnLobbyState;
            EventManager.OnGameStarted += OnGameState;
            EventManager.OnGameEnded += OnGameEndState;
        }

        private void OnDisable()
        {
            EventManager.OnMenu -= OnMenuState;
            EventManager.OnLobbyEntered -= OnLobbyState;
            EventManager.OnGameStarted -= OnGameState;
            EventManager.OnGameEnded -= OnGameEndState;
        }

        #endregion
        
        #region State Handlers
        
        private void OnMenuState()
        {
            Debug.Log("HEREEEE");
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(true);
                
            }
            
            if (launchGamePanel != null)
            {
                launchGamePanel.SetActive(false);
            }
            
            mainBackground.SetActive(true);
        }
        
        private void OnLobbyState()
        {
            
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
            
            if (launchGamePanel != null)
            {
                launchGamePanel.SetActive(IsLocalPlayerHost());
            }
            
            mainBackground.SetActive(false);
        }
        
        private void OnGameState()
        {
            
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
            
            if (launchGamePanel != null)
            {
                launchGamePanel.SetActive(false);
            }
            
            mainBackground.SetActive(false);
        }
        
        
        //TODO CHECK IF SEKER OR HIDER WON
        private void OnGameEndState()
        {
            
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
            
            if (launchGamePanel != null)
            {
                launchGamePanel.SetActive(false);
            }
            
            mainBackground.SetActive(false);

            if (GameManager.Instance.CheckIfSeekerWon())
            {
                Debug.Log("Seeker Won");
            }
            else
            {
                Debug.Log("Hider Won");
            }
        }

        private void LaunchGame()
        {
            if (Keyboard.current.fKey.wasPressedThisFrame && GameManager.Instance != null)
            {
                GameManager.Instance.LaunchGame();
            }
        }
        
        #endregion

        #region Helpers

        private bool IsLocalPlayerHost()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        }

        #endregion
    }
}