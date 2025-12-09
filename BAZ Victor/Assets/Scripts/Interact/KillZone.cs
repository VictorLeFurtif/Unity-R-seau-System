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
            if (!NetworkManager.Singleton.IsServer) return;
            
            if (other.CompareTag("Hider"))
            {
                PlayerGameBehavior hider = other.GetComponentInParent<PlayerGameBehavior>();
                
                if (hider == null) 
                    hider = other.GetComponent<PlayerGameBehavior>();
                
                if (hider != null && canKill && !hider.IsImprisoned())
                {
                    if (GameManager.Instance.CheckIfAddPrisonnerEndGame())
                    {
                        Debug.Log("1");
                        GameManager.Instance.NotifyGameEndClientRpc(true);
                    }
                    else
                    {
                        Debug.Log("2");
                        hider.SetImprisoned(true);
                    }
                    
                }
            }

            if (other.CompareTag("Seeker"))
            {
                PlayerGameBehavior seeker = other.GetComponentInParent<PlayerGameBehavior>();
                
                if (seeker == null) 
                    seeker = other.GetComponent<PlayerGameBehavior>();
                
                if (seeker != null && canKill && seeker.IsSpawned)
                {
                    GameManager.Instance.NotifyGameEndClientRpc(false);
                }
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