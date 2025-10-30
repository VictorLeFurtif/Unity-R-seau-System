using System;
using System.Collections;
using System.Collections.Generic;
using EventBus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manager
{
    public class PrisonZone : NetworkBehaviour
    {
        #region Fields

        private float defaultProgression = 10f;

        private NetworkVariable<float> releaseProgression = new NetworkVariable<float>(10f);
        private NetworkVariable<int> prisonerCount = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone);
        [SerializeField] private NetworkVariable<bool> releasing = new NetworkVariable<bool>(false);
        
        private Queue<PlayerGameBehavior> prisonerQueue = new Queue<PlayerGameBehavior>();
        [SerializeField] private List<PlayerGameBehavior> hiderReleasing = new List<PlayerGameBehavior>();

        private Coroutine checkForEndGame;
        #endregion

        #region Unity Methods

        private void Update()
        {
            if (!IsServer) return;  //on met a jour la networkVariable que sur le host/serv
            
            TryReleasingPlayer();

            if (Keyboard.current.uKey.wasPressedThisFrame && IsOwner)
            {
                prisonerCount.Value++;
            }
        }

        #endregion

        #region Physics Methods

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !other.CompareTag("Hider"))
            {
                Debug.Log("You re not a hider");
                return;
            }
            
            PlayerGameBehavior hider = other.GetComponentInParent<PlayerGameBehavior>();

            if (hider == null || hider.IsImprisoned() || hiderReleasing.Contains(hider)) 
            {
                return;
            }
            
            hiderReleasing.Add(hider);
        
            if (hiderReleasing.Count == 1 && prisonerQueue.Count > 0)
            {
                Debug.Log("You Start prison");
                StartReleasePlayer();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer || !other.CompareTag("Hider")) return;
            
            PlayerGameBehavior hider = other.GetComponentInParent<PlayerGameBehavior>();
            
            if (hider == null || hider.IsImprisoned()) return;
            
            Debug.Log("Exit");
            hiderReleasing.Remove(hider);

            if (hiderReleasing.Count <= 0)
            {
                StopTryReleasePlayer();
            }
        }

        #endregion

        #region Prison Methods
        
        public void AddPrisoner(PlayerGameBehavior prisoner)
        {
            if (!IsServer) return; 
            
            prisonerQueue.Enqueue(prisoner);
            prisonerCount.Value = prisonerQueue.Count;

            if (hiderReleasing.Contains(prisoner)) //was releasing someone
            {
                hiderReleasing.Remove(prisoner);
            }
            
            EventManager.PlayerIsImprisoned();
            
            //GameManager.Instance.CheckIfEndGame(prisonerCount.Value);
        }

        
        private void ResetZoneAfterRelease()
        {
            releaseProgression.Value = defaultProgression;
        }

        private void ResetZone()
        {
            ResetZoneAfterRelease();
            releasing.Value = false;
        }

        #endregion

        #region Release Methods
        
        private void StartReleasePlayer()
        {
            if (prisonerQueue.Count == 0) return; 
    
            releasing.Value = true;
            releaseProgression.Value = defaultProgression;
        }

        private void TryReleasingPlayer()
        {
            if (!releasing.Value || prisonerQueue.Count == 0) return;

            releaseProgression.Value -= Time.deltaTime;
            
            if (releaseProgression.Value <= 0)
            {
                ReleasePlayer();
            }
        }
        
        private void ReleasePlayer()
        {
            PlayerGameBehavior freedPrisoner = prisonerQueue.Dequeue();
            freedPrisoner.SetImprisoned(false); 
            
            prisonerCount.Value = prisonerQueue.Count; 
            ResetZoneAfterRelease();
        }
        
        private void StopTryReleasePlayer()
        {
            releaseProgression.Value = defaultProgression;
            releasing.Value = false;
        }

        #endregion

        #region Getter Setter

        public int GetPrisonerCount() => prisonerCount.Value; 
        public float GetReleaseProgress() => releaseProgression.Value;
        public bool IsReleasing() => releasing.Value;

        #endregion

        #region Security and Check

        private void CheckPrisonState()
        {
            if (releasing.Value && hiderReleasing.Count <= 0 )
            {
                ResetZone();
            }
        }

        #endregion

        #region Observer
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                EventManager.OnPlayerImprisoned += CheckPrisonState;
                prisonerCount.OnValueChanged += GameManager.Instance.CheckIfEndGame;
            }
          
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsServer)
            {
                EventManager.OnPlayerImprisoned -= CheckPrisonState;
            }
            
        }

        #endregion
    }
}