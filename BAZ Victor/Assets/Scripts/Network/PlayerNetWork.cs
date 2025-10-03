using System;
using Struct;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;


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

        [Header("Network Variable")] private NetworkVariable<int> randomNumber
            = new (1,NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        private NetworkVariable<PlayerData> playerData = new(new PlayerData()
        {
            life = 100,
            stunt = false,
        },NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

        #endregion

        #region Unity Methods

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //randomNumber.Value = Random.Range(0, 100);
                /*
                playerData.Value = new PlayerData()
                {
                    life = Random.Range(0, 100),
                    stunt = playerData.Value.stunt,
                    message = "Praise the sun"
                };*/
                
                TestRpc(new RpcParams());
            }
            
            Shift();
        }
        
        #endregion


        #region PlayerNetwork Methods

        private void Shift()
        {
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

        
        [Rpc(SendTo.NotServer)]
        private void TestRpc(RpcParams rpcParams)
        {
            Debug.Log("TestRpc " + OwnerClientId + "RPC params : " +rpcParams.Receive.SenderClientId);
        }

        #endregion

        #region Observer

        public override void OnNetworkSpawn()
        {
            /*
            randomNumber.OnValueChanged += (int previousValue, int newValue) =>
            {
                Debug.Log(OwnerClientId + " Random Number : " + randomNumber.Value);
            };*/

            playerData.OnValueChanged += (PlayerData previousValue, PlayerData newValue) =>
            {
                Debug.Log("ID : "+ OwnerClientId + " life : " + playerData.Value.life + " " +
                          "stunt : " + playerData.Value.stunt + " Message : " + playerData.Value.message);
            };
        }

        #endregion
    }
}

