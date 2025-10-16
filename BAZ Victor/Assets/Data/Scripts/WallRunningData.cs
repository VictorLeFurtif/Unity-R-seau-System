using UnityEngine;

namespace Data.Scripts
{
    [CreateAssetMenu(menuName = "WallRunning Data",fileName = "wallRunningData")]
    public class WallRunningData : ScriptableObject
    {
        [field: SerializeField] public LayerMask WallLayer { get; private set; }
        [field: SerializeField] public LayerMask GroundLayer{ get; private set; }
        [field: SerializeField] public float WallRunForce{ get; private set; }
        [field: SerializeField] public float MaxWallRunTime{ get; private set; }
        
        [field: SerializeField] public float WallClimbSpeed{ get; private set; }
        [field: SerializeField] public float WallJumpUpForce{ get; private set; }
        [field: SerializeField] public float WallJumpSideForce{ get; private set; }
        
        [field: SerializeField] public float WallCheckDistance{ get; private set; }
        [field: SerializeField] public float MinJumpHeight{ get; private set; }
        
        [field: SerializeField] public float ExitWallTime{ get; private set; }
        
        [field: SerializeField] public bool UseGravity{ get; private set; }
        
        [field: SerializeField] public float GravityCounterForce{ get; private set; }
    }
}