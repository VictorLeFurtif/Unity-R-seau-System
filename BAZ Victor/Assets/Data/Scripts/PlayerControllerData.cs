using UnityEngine;

namespace Data.Scripts
{
    [CreateAssetMenu(menuName = "Player Controller Data",fileName = "playerControllerData")]
    public class PlayerControllerData : ScriptableObject
    {
        [Header("SPEED")] 
        [field: SerializeField] public float SpeedIncreaseMultiplier { get; private set; }
        [field:SerializeField] public float SlopeIncreaseMultiplier { get; private set; }
        
        [field: SerializeField] public float WalkSpeed { get; private set; }
        [field: SerializeField] public float SprintSpeed { get; private set; }
        [field: SerializeField] public float SlideSpeed { get; private set; }
        [field: SerializeField] public float WallRunningSpeed { get; private set; }
        
        [field: SerializeField] public float GroundDrag { get; private set; }
        
        [field: SerializeField] public float CrouchSpeed { get; private set; }
        [field: SerializeField] public float CrouchYScale { get; private set; }
        
        [field: SerializeField] public float JumpForce { get; private set; }
        [field: SerializeField] public float JumpCooldown { get; private set; }
        [field: SerializeField] public float AirMultiplier { get; private set; }
        
        [field: SerializeField] public float PlayerHeight { get; private set; }
        
        [field: SerializeField] public LayerMask GroundLayer { get; private set; }
        
        [field: SerializeField] public float MaxSlopeAngle { get; private set; }
        
        [field: SerializeField] public float SensX { get; private set; }
        [field: SerializeField] public float SensY { get; private set; }
    }
}