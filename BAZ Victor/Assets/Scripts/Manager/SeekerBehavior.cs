using System;
using EventBus;
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
        private bool attackEnabled = false;

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
            
            EventManager.OnLobbyEntered += OnLobbyEntered;
            EventManager.OnGameStarted += OnGameStarted;
            EventManager.OnGameEnded += OnGameEnded;
            
            inputActions.Enable();
            inputActions.Player.Attack.performed +=  Attack;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (!IsOwner || inputActions == null) return;
            
            EventManager.OnLobbyEntered -= OnLobbyEntered;
            EventManager.OnGameStarted -= OnGameStarted;
            EventManager.OnGameEnded -= OnGameEnded;

            inputActions.Player.Attack.performed -=  Attack;
            inputActions.Disable();
            inputActions.Dispose();
        }

        #endregion
        
        #region Game State Handlers
        
        private void OnLobbyEntered()
        {
            attackEnabled = false;
        }
        
        private void OnGameStarted()
        {
            attackEnabled = true;
        }
        
        private void OnGameEnded()
        {
            attackEnabled = false;
        }
        
        #endregion
        
        #region Attack
        
        private RaycastHit hit;
        
        private void Attack(InputAction.CallbackContext ctx)
        {

            if (isAttacking || !attackEnabled) return;

            isAttacking = true;
            Invoke(nameof(ResetAttack),attackResetTime);
            
            if (Physics.Raycast(orientation.position, orientation.forward, out hit, rangeAttack, ~seekerLayer))
            {
                PlayerGameBehavior hitPlayer = hit.transform.GetComponent<PlayerGameBehavior>();
                
                if (hitPlayer != null && !hitPlayer.IsSeeker() && !hitPlayer.IsImprisoned()) 
                {
                    hitPlayer.SetImprisoned(true);
                    return;
                }
            }
        }

        private void ResetAttack()
        {
            isAttacking = false;
        }
        

        #endregion
    }
}
