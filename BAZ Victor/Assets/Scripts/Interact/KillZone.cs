using System;
using EventBus;
using Manager;
using Unity.Netcode;
using UnityEngine;

namespace Interact
{
    public class KillZone : MonoBehaviour
    {

        #region Fields

        private bool canKill;

        #endregion

        #region Physics

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Hider"))
            {
                PlayerGameBehavior hider = GetComponentInParent<PlayerGameBehavior>();
                if (canKill)
                {
                    hider.SetImprisoned(true);
                }
                else
                {
                    //hider.transform.position
                }
            }

            if (other.CompareTag("Seeker"))
            {
                PlayerGameBehavior seeker = GetComponentInParent<PlayerGameBehavior>();
                //TODO faire spawn bon endroit
            }
        }

        #endregion
        
        #region Observer

        private void OnEnable()
        {
            EventManager.OnLobbyEntered += OnLobbyEntered;
            EventManager.OnGameStarted += OnGameStarted;
            EventManager.OnGameEnded += OnGameEnded;
        }

        private void OnDisable()
        {
            EventManager.OnLobbyEntered -= OnLobbyEntered;
            EventManager.OnGameStarted -= OnGameStarted;
            EventManager.OnGameEnded -= OnGameEnded;
        }

        #endregion
        
        #region Game State Handlers
        
        private void OnLobbyEntered()
        {
            canKill = false;
        }
        
        private void OnGameStarted()
        {
            canKill = true;
        }
        
        private void OnGameEnded()
        {
            canKill = false;
        }
        
        #endregion
    }
}
