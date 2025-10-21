using System;
using System.Collections;
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
        [SerializeField] private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>(false);

        private PlayerController pc;

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
            //TeleportPrison();
            StartCoroutine(TeleportPrisonIe());
            
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

        private IEnumerator TeleportPrisonIe()
        {
            GameObject prison = GameObject.FindWithTag("Prison");
            Vector3 prisonPos = prison.transform.position + new Vector3(0,2,0); //offsett ?

            Rigidbody playerRb = pc.GetPlayerRigidbody();
            
            playerRb.isKinematic = true;
            yield return new WaitForFixedUpdate();
            transform.position = prisonPos;
            yield return new WaitForFixedUpdate();
            playerRb.isKinematic = false;
            
            pc.ResetVelocity();
            transform.position = prisonPos; //to make sure
            
            PrisonZone prisonZone = prison.GetComponent<PrisonZone>();
            prisonZone.AddPrisoner(this);
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
