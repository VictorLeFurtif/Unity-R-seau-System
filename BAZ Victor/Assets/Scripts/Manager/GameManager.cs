using System;
using System.Collections;
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

        private NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(
            GameState.Menu,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private List<ulong> connectedPlayerIds = new List<ulong>();

        private NetworkVariable<int> numberConnectedPlayer = new NetworkVariable<int>();

        [SerializeField] private PrisonZone prison;

        [SerializeField] private float defaultTimerWinHider;
        [SerializeField] private NetworkVariable<float> timerWinHider = new NetworkVariable<float>();
        private bool timerHiderRunning = false;

        private Coroutine finishGameCoroutine;

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
                
                timerWinHider.Value = defaultTimerWinHider;

                EventManager.OnGameEnded += OnEndGame;
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
                
                EventManager.OnGameEnded -= OnEndGame;
            }
        }

        //TODO CHANGE SYSTEM BY ADDING HERE THE LOGIC
        private void Update()
        {
            if (!IsServer || !timerHiderRunning) return;
    
            timerWinHider.Value -= Time.deltaTime;
    
            if (timerWinHider.Value <= 0) 
            {
                timerHiderRunning = false;
        
                int prisoners = prison.GetPrisonerCount();
        
                if ((numberConnectedPlayer.Value - 1) == prisoners)
                {
                    NotifyGameEndClientRpc(true);
                }
                else
                {
                    NotifyGameEndClientRpc(false);
                }
        
                ChangeGameState(GameState.GameEnd);
            }
        }

        #endregion

        #region State Management
        
        public void ChangeGameState(GameState newGameState)
        {
            if (!IsServer) return; 
            
            currentGameState.Value = newGameState;
        }
        
        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            EventManager.GameStateChanged(newState);
        }

        public void CheckIfEndGame(int currentPrisonerCount)
        {
            if ((numberConnectedPlayer.Value - 1) == currentPrisonerCount)
            {
                timerHiderRunning = false;
        
                NotifyGameEndClientRpc(
                    seekerWon: true
                );
        
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
            timerHiderRunning = true;
            timerWinHider.Value = defaultTimerWinHider;
        }

        public void ToggleTimerHider(bool _result)
        {
            if (IsServer)
            {
                timerHiderRunning = _result;
            }
        }

        #endregion

        #region Getters
        
        public bool CheckIfSeekerWon() => (numberConnectedPlayer.Value - 1) == prison.GetPrisonerCount();

        public GameState GetCurrentState() => currentGameState.Value;
        public int GetPlayerCount() => numberConnectedPlayer.Value;
        public bool CanStartGame() => connectedPlayerIds.Count >= minPlayersToStart;

        public bool InGame() => currentGameState.Value == GameState.InGame;

        public int PlayerInPrison() => prison.GetPrisonerCount();

        public float DefaultValueTimerHider() => defaultTimerWinHider;

        public float GetTimerHider() => timerWinHider.Value;

        #endregion

        #region State Management

        private void OnEndGame()
        {
            finishGameCoroutine ??= StartCoroutine(OnEndGameCoroutine());
        }

        private IEnumerator OnEndGameCoroutine()
        {
            yield return new WaitForSeconds(5);
            ChangeGameState(GameState.Lobby);
        }

        #endregion

        #region Rpc

        

        [ClientRpc]
        private void NotifyClientOfGameStateClientRpc(GameState state, ClientRpcParams rpcParams = default)
        {
            EventManager.GameStateChanged(state);
        }

        [ClientRpc]
        private void NotifyGameEndClientRpc(bool seekerWon)
        {
            EventManager.GameEnded(seekerWon);
        }

        #endregion
    }
}