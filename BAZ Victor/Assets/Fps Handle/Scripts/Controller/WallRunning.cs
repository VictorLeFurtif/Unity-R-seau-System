using Unity.Netcode;
using UnityEngine;

namespace Fps_Handle.Scripts.Controller
{
    public class WallRunning : NetworkBehaviour
    {
        #region Fields

        [Header("Wall Running")] 
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float wallRunForce;
        [SerializeField] private float maxWallRunTime;
        private float wallrunTimer;
        [SerializeField] private float wallClimbSpeed;
        [SerializeField] private float wallJumpUpForce;
        [SerializeField] private float wallJumpSideForce;

        [Header("Input")] 
        private float horizontalInput;
        private float verticalInput;
        private bool upwardRunning;
        private bool downwardRunning;
        

        [Header("Detection")] 
        [SerializeField] private float wallCheckDistance;
        [SerializeField] private float minJumpHeight;
        private RaycastHit leftWallhit;
        private RaycastHit rightWallhit;
        private bool wallLeft;
        private bool wallRight;

        [Header("References")]
        [SerializeField] private Transform orientation;
        private PlayerController pc;
        private Rigidbody rb;
        

        [Header("Exiting")] 
        private bool exitingWall;
        [SerializeField] private float exitWallTime;
        private float exitWallTimer;

        [Header("Gravity")] 
        [SerializeField] private bool useGravity;

        [SerializeField] private float gravityCounterForce;

        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private bool jumpPressed;
        
        #endregion

        #region Unity Methods
        

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner)
            {
                return;
            }
            
            inputActions = new PlayerInputActions();
            
            inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            
            inputActions.Player.Jump.performed += ctx => jumpPressed = true;
            inputActions.Player.Jump.canceled += ctx => jumpPressed = false;
            
            inputActions.Player.Sprint.performed += ctx => upwardRunning = true;
            inputActions.Player.Sprint.canceled += ctx => upwardRunning = false;
            
            inputActions.Player.Slide.performed += ctx => downwardRunning = true;
            inputActions.Player.Slide.canceled += ctx => downwardRunning = false;
            
            inputActions.Enable();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsOwner && inputActions != null)
            {
                inputActions.Disable();
                inputActions.Dispose(); 
                inputActions = null;
            }
        }
        
        

        private void Start()
        {
            InitComponent();
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }
            
            CheckForWall();
            StateMachine();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }
            
            if (pc.GetWallRunning())
            {
                WallRunningMovement();
            }
        }

        #endregion

        #region Init

        private void InitComponent()
        {
            rb = GetComponent<Rigidbody>();
            pc = GetComponent<PlayerController>();
        }

        #endregion

        #region Wall Running Method

        private void CheckForWall()
        {
            wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, wallLayer);
            wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, wallLayer);
        }

        private bool AboveGround()
        {
            return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayer);
        }

        private void StateMachine()
        {
            InputWallRunning();

            if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
            {
                if (!pc.GetWallRunning())
                {
                    StartWallRunning();
                }

                if (wallrunTimer > 0 )
                {
                    wallrunTimer -= Time.deltaTime;
                }

                if (wallrunTimer <= 0 && pc.GetWallRunning())
                {
                    exitingWall = true;
                    exitWallTimer = exitWallTime;
                }
                
                if (jumpPressed)
                {
                    WallJump();
                }
            }
            else if (exitingWall)
            {
                if (pc.GetWallRunning())
                {
                    StopWallRunning();
                }

                if (exitWallTimer > 0)
                {
                    exitWallTimer -= Time.deltaTime;
                }

                if (exitWallTimer <= 0 )
                {
                    exitingWall = false;
                }
            }
            
            else
            {
                if (pc.GetWallRunning())
                {
                    StopWallRunning();
                }
            }
        }

        private void InputWallRunning()
        {
            horizontalInput = moveInput.x;
            verticalInput = moveInput.y;
        }

        private void StartWallRunning()
        {
            wallrunTimer = maxWallRunTime;
            pc.SetterBoolWallRunning(true);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            
            CameraController.Instance.DoFov(90f);
            
            
            if (wallLeft)
            {
                CameraController.Instance.DoTile(-5f);
            }

            if (wallRight)
            {
                CameraController.Instance.DoTile(5f);
            }

            if (IsOwner)
            {
                StartWallRunningRpc();
            }
        }

        private void StopWallRunning()
        {
            CameraController.Instance.DoFov(80f);
            CameraController.Instance.DoTile(0);
            pc.SetterBoolWallRunning(false);
            
            if (IsOwner)
            {
                StopWallRunningRpc();
            }
        }

        private void WallRunningMovement()
        {
            rb.useGravity = useGravity;
            
            Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((orientation.forward - wallForward).magnitude > (orientation.forward- -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }
            
            rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

            if (upwardRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x,wallClimbSpeed, rb.linearVelocity.z);
            }
            
            if (downwardRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x,-wallClimbSpeed, rb.linearVelocity.z);
            }
            
            if (!(wallLeft && horizontalInput > 0 ) && !(wallRight && horizontalInput < 0 ))
            {
                rb.AddForce(-wallNormal * 100,ForceMode.Force);
            }

            if (useGravity)
            {
                rb.AddForce(transform.up * gravityCounterForce,ForceMode.Force);
            }
        }
        
        #endregion

        #region Wall Jump

        private void WallJump()
        {
            exitingWall = true;
            exitWallTimer = exitWallTime;
            
            Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
            Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(forceToApply,ForceMode.Impulse);
        }

        #endregion

        #region Network Methods

        [Rpc(SendTo.NotMe)]
        private void StartWallRunningRpc()
        {
            pc.SetterBoolWallRunning(true);
        }
        
        [Rpc(SendTo.NotMe)]
        private void StopWallRunningRpc()
        {
            pc.SetterBoolWallRunning(false);
        }

        #endregion
    }
}