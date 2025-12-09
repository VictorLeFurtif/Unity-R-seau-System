using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class CustomPlayerSpawner : NetworkBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject seekerPrefab;
        [SerializeField] private GameObject hiderPrefab;

        private void OnEnable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected; 
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            
            if (!IsOwner)
            {
                return;
            }
            
            var prefabToSpawn = NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId
                ? seekerPrefab
                : hiderPrefab;

            var playerInstance = Instantiate(prefabToSpawn);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            
        }
    }
}