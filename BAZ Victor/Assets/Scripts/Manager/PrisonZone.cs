using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class PrisonZone : NetworkBehaviour
    {
        #region Fields

        private float defaultProgression = 10f;

        private NetworkVariable<float> releaseProgression = new NetworkVariable<float>(10f);
        private NetworkVariable<int> prisonerCount = new NetworkVariable<int>(0);
        private NetworkVariable<bool> releasing = new NetworkVariable<bool>(false);
        
        private Queue<PlayerGameBehavior> prisonerQueue = new Queue<PlayerGameBehavior>();
        private List<PlayerGameBehavior> hiderReleasing = new List<PlayerGameBehavior>();

        #endregion

        #region Unity Methods

        private void Update()
        {
            if (!IsServer) return;  //on met a jour la networkVariable que sur le host/serv
            
            TryReleasingPlayer();
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
    
            PlayerGameBehavior hider = other.GetComponent<PlayerGameBehavior>();

            if (hider == null || hider.IsImprisoned() || hiderReleasing.Contains(hider))
            {
                Debug.Log("You re certainly already in prison");
                return;
            }
            
            hiderReleasing.Add(hider);
        
            if (hiderReleasing.Count == 1 && prisonerQueue.Count > 0)
            {
                StartReleasePlayer();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer || !other.CompareTag("Hider")) return;
            
            PlayerGameBehavior hider = other.GetComponent<PlayerGameBehavior>();
            
            if (hider == null || hider.IsImprisoned()) return;
            
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
        }
        
        private void StartReleasePlayer()
        {
            if (prisonerQueue.Count == 0) return; 
    
            releasing.Value = true;
            releaseProgression.Value = defaultProgression;
        }
        
        private void StopTryReleasePlayer()
        {
            releaseProgression.Value = defaultProgression;
            releasing.Value = false;
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
        
        private void ResetZoneAfterRelease()
        {
            releaseProgression.Value = defaultProgression;
        }

        #endregion

        #region Getter Setter

        public int GetPrisonerCount() => prisonerCount.Value; 
        public float GetReleaseProgress() => releaseProgression.Value; 

        #endregion
    }
}