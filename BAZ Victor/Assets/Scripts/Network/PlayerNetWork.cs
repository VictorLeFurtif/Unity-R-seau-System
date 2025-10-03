using System;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class PlayerNetWork : NetworkBehaviour
    {
        #region Fields
        [Header("Parameters")]
        
        [SerializeField] private float moveSpeed = 10f;
        private Vector3 direction;

        [Header("Key")] [SerializeField] private KeyCode leftKey = KeyCode.A;
        [Header("Key")] [SerializeField] private KeyCode rightKey = KeyCode.D;
        [Header("Key")] [SerializeField] private KeyCode upKey = KeyCode.W;
        [Header("Key")] [SerializeField] private KeyCode downKey = KeyCode.S;
        

        #endregion

        #region Unity Methods

        private void Update()
        {
            Shift();
        }
        
        #endregion


        #region PlayerNetwork Methods

        private void Shift()
        {
            if (!IsOwner)
            {
                return;
            }
            
            direction = Vector3.zero;

            if (Input.GetKey(leftKey))
            {
                direction.x = -1;
            }
            if (Input.GetKey(rightKey))
            {
                direction.x = 1;
            }
            if (Input.GetKey(upKey))
            {
                direction.z = 1;
            }
            if (Input.GetKey(downKey))
            {
                direction.z = -1;
            }

            direction = direction.normalized;

            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        #endregion
    }
}
