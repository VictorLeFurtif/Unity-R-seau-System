using System;
using Enum;
using UnityEngine;

namespace EventBus
{
    public static class EventManager
    {

        #region Action

        public static event Action<GameState> OnGameStateChanged;
        
        public static event Action OnLobbyEntered;
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;
        public static event Action OnMenu;

        #endregion
        


        #region Methods Helper 

        public static void GameStateChanged(GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
            
            switch (newState)
            {
                case GameState.Menu:
                    OnMenu?.Invoke();
                    break;
                case GameState.Lobby:
                    OnLobbyEntered?.Invoke();
                    break;
                case GameState.InGame:
                    OnGameStarted?.Invoke();
                    break;
                case GameState.GameEnd:
                    OnGameEnded?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        #endregion
    }
}
