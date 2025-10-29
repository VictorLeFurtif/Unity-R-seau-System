using System;
using System.Collections.Generic;
using Enum;
using EventBus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manager
{
    public class GameManager : NetworkBehaviour
    {
        #region Singleton
        
        public static GameManager Instance { get; private set; }
        
        #endregion
        
        #region Fields

        [Header("Game Settings")]
        [SerializeField] private int minPlayersToStart = 2;

        [SerializeField] private NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(
            GameState.Menu,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private List<ulong> connectedPlayerIds = new List<ulong>();

        private NetworkVariable<int> numberConnectedPlayer = new NetworkVariable<int>();

        [SerializeField] private PrisonZone prison;

        #endregion
        
        #region Unity Methods
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            ChangeGameState(GameState.Menu);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            currentGameState.OnValueChanged += OnGameStateChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                ChangeGameState(GameState.Lobby);
            }
        }


        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            currentGameState.OnValueChanged -= OnGameStateChanged;
            
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.LogWarning("actual game state : " + currentGameState.Value + "\n isServer : " + IsServer);
            }
        }

        #endregion

        #region State Management
        
        public void ChangeGameState(GameState newGameState)
        {
            if (!IsServer) return; 
            
            //if (currentGameState.Value == newGameState) return; 
            
            currentGameState.Value = newGameState;
        }
        
        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            EventManager.GameStateChanged(newState);
        }

        public void CheckIfEndGame(int catchPlayer)
        {
            if ((numberConnectedPlayer.Value - 1) ==  catchPlayer)
            {
                ChangeGameState(GameState.GameEnd);
            }
        }

        #endregion

        #region Network Callbacks

        private void OnClientConnected(ulong clientId)
        {
            if (!connectedPlayerIds.Contains(clientId))
            {
                connectedPlayerIds.Add(clientId);
                numberConnectedPlayer.Value++;
            }
                

            NotifyClientOfGameStateClientRpc(currentGameState.Value, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
        }

        private void OnClientDisconnected(ulong clientId)
        {
            connectedPlayerIds.Remove(clientId);
            numberConnectedPlayer.Value--;
            
            if (currentGameState.Value == GameState.InGame && connectedPlayerIds.Count < minPlayersToStart)
            {
                ChangeGameState(GameState.Lobby);
            }
        }

        #endregion

        #region Game Actions

        public void LaunchGame()
        {
            if (!IsServer)
            {
                Debug.LogWarning("Only server can launch game!");
                return;
            }
            
            if (connectedPlayerIds.Count < minPlayersToStart)
            {
                Debug.LogWarning($"Not enough players! Need {minPlayersToStart}, have {connectedPlayerIds.Count}");
                return;
            }
            
            Debug.Log("Launching game!");
            ChangeGameState(GameState.InGame);
        }

        #endregion

        #region Getters
        
        public bool CheckIfSeekerWon() => (numberConnectedPlayer.Value - 1) == prison.GetPrisonerCount();

        public GameState GetCurrentState() => currentGameState.Value;
        public int GetPlayerCount() => numberConnectedPlayer.Value;
        public bool CanStartGame() => connectedPlayerIds.Count >= minPlayersToStart;

        public bool InGame() => currentGameState.Value == GameState.InGame; 

        #endregion

        #region Rpc

        

        [ClientRpc]
        private void NotifyClientOfGameStateClientRpc(GameState state, ClientRpcParams rpcParams = default)
        {
            EventManager.GameStateChanged(state);
        }


        #endregion
    }
}