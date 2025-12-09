using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class SpawnManager : NetworkBehaviour
    {
        #region Singleton
        
        public static SpawnManager Instance { get; private set; }
        
        #endregion
        
        #region Fields
        
        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPointsInGame = new Transform[8]; 
        [SerializeField] private Transform[] spawnPointsLobby = new Transform[8]; 
        
        private List<int> availableSpawnIndices = new List<int>();
        
        #endregion
        
        #region Unity Methods
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
        }
        
        #endregion
        
        #region Spawn Logic
        
        public void InitializeSpawns()
        {
            if (!IsServer) return;
            
            availableSpawnIndices.Clear();
            for (int i = 0; i < spawnPointsInGame.Length; i++)
            {
                availableSpawnIndices.Add(i);
            }
        }
        
        public Vector3 GetSpawnPosition(bool toLobby)
        {
            if (!IsServer)return Vector3.zero;
            
            if (availableSpawnIndices.Count == 0)
            {
                InitializeSpawns(); 
            }
            
            int randomIndex = Random.Range(0, availableSpawnIndices.Count);
            int spawnIndex = availableSpawnIndices[randomIndex];
            
            availableSpawnIndices.RemoveAt(randomIndex);
            
            return toLobby ? spawnPointsLobby[spawnIndex].position : spawnPointsInGame[spawnIndex].position;
        }
        
        #endregion
    }
}