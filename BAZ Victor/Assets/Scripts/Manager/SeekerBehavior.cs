using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manager
{
    public class SeekerBehavior : NetworkBehaviour
    {
        #region Fields

        [SerializeField] private Transform orientation;
        [SerializeField] private float rangeAttack;

        private bool isAttacking = false;

        [SerializeField] private float attackResetTime = 1f;
        
        private PlayerInputActions inputActions;

        [SerializeField] private LayerMask seekerLayer;
        
        #endregion

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }

        #region Observer

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner) return;
            inputActions.Enable();
            inputActions.Player.Attack.performed +=  Attack;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (!IsOwner || inputActions == null) return;

            inputActions.Player.Attack.performed -=  Attack;
            inputActions.Disable();
            inputActions.Dispose();
        }

        #endregion
        
        #region Attack
        
        private RaycastHit hit;
        
        private void Attack(InputAction.CallbackContext ctx)
        {

            if (isAttacking) return;

            isAttacking = true;
            Invoke(nameof(ResetAttack),attackResetTime);
            
            if (Physics.Raycast(orientation.position, orientation.forward, out hit, rangeAttack, ~seekerLayer))
            {
                PlayerGameBehavior hitPlayer = hit.transform.GetComponent<PlayerGameBehavior>();
                
                //hit a hider not in prison
                if (hitPlayer != null && !hitPlayer.IsSeeker() && !hitPlayer.IsImprisoned()) 
                {
                    Debug.Log("YOU HIT A PLAYER WELL DONE");
                    hitPlayer.SetImprisoned(true);
                    return;
                }
                
                Debug.Log("YOU FAIL");
            }
        }

        private void ResetAttack()
        {
            isAttacking = false;
        }
        

        #endregion
    }
}
