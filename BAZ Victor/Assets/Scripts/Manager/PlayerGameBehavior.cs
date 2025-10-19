using System;
using Fps_Handle.Scripts.Controller;
using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class PlayerGameBehavior : NetworkBehaviour
    {
        #region Fields

        private NetworkVariable<bool> isSeeker = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>(false);
        private NetworkVariable<float> releaseProgress = new NetworkVariable<float>(0f);

        private PlayerController pc;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
        }

        #endregion
        
        #region State Methods
        
        public void SetImprisoned(bool value) //true if tag and false if released by friend
        {
            SetImprisonedRpc(value);
        }
        
        // We just tp to prison and block movement
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
        
        /*
         If I understand the logic is that because we dont use a RPC but a networkVariable + Obeserver pattern with
         the on valueChanged then it will be updated in all scene thanks to NV and then the call will be made
         then the player who has been it will be tp in every scene.
        */

        private void TeleportPrison()
        {
            GameObject prison = GameObject.FindWithTag("Prison");
            
            Vector3 prisonPos = prison.transform.position;
            transform.position = prisonPos;

            PrisonZone prisonZone = prison.GetComponent<PrisonZone>();
            prisonZone.AddPrisoner(this);
            
            pc.ResetVelocity();
        }
        
        #endregion

        #region RPC

        [Rpc(SendTo.Everyone)]
        private void SetImprisonedRpc(bool value)
        {
            isImprisoned.Value = value;
        
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
