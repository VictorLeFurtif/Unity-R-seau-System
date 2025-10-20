using System;
using Enum;
using Fps_Handle.Scripts.Controller;
using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class PlayerGameBehavior : NetworkBehaviour
    {
        #region Fields

        [SerializeField] private NetworkVariable<bool> isSeeker = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>(false);

        private PlayerController pc;

        private PlayerGameBehaviorState currentPlayerGameBehaviorState;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
        }

        #endregion
        
        #region State Methods
        
        public void SetImprisoned(bool value)
        {
            SetImprisonedRpc(value);
        }
        
        private void OnCapturePrison()
        {
            TeleportPrison();

            if (IsOwner)
            {
                pc.SetterMove(false);
            }
        }

        private void OnReleasePrison()
        {
            if (IsOwner)
            {
                pc.SetterMove(true);
            }
        }

        #endregion

        #region Teleport In Prison

        private void TeleportPrison()
        {
            GameObject prison = GameObject.FindWithTag("Prison");
            Vector3 prisonPos = prison.transform.position + new Vector3(0,2,0); //offsett ?

            pc.GetPlayerRigidbody().isKinematic = true;
            transform.position = prisonPos;
            pc.GetPlayerRigidbody().isKinematic = false;
            
            PrisonZone prisonZone = prison.GetComponent<PrisonZone>();
            prisonZone.AddPrisoner(this);
            
            pc.ResetVelocity();
        }
        
        #endregion

        #region RPC

        [Rpc(SendTo.Everyone)]
        private void SetImprisonedRpc(bool value)
        {
            if (IsServer) //on met a jour que le serv/host vu que c'est une networkVariable
            {
                isImprisoned.Value = value;
            }
    
            //on met a jour que la pos pour tout le monde en suite
            if (value)
                OnCapturePrison();
            else
                OnReleasePrison();
        }

        #endregion

        #region Getter Setter

        public bool IsSeeker() => isSeeker.Value;

        public bool IsImprisoned() => isImprisoned.Value;

        #endregion
    }
}
