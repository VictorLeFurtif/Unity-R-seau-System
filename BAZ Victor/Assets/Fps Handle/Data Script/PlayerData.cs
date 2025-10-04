using System;
using UnityEngine;

namespace Data_Script
{
    [CreateAssetMenu(menuName = "SO/PlayerData",fileName = "player data")]
    public class PlayerData : ScriptableObject
    {
        [field: Header("Sprint Speed"), SerializeField]
        public float SprintSpeed { get; private set; }
        [field: Header("Walk Speed"), SerializeField]
        public float WalkSpeed { get; private set; }

        public PlayerDataInstance Instance()
        {
            return new PlayerDataInstance(this);
        }

        
    }

    [Serializable]
    public class PlayerDataInstance
    {
        public float sprintSpeed;
        public float walkSpeed;

        public PlayerDataInstance(PlayerData data)
        {
            sprintSpeed = data.SprintSpeed;
            walkSpeed = data.WalkSpeed;
        }
    }
}
