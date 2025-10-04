using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fps_Handle.Scripts.Controller
{
    public class PlayerController : NetworkBehaviour
    {
        #region Variable
        
        [Header("Movement")] 
        private float moveSpeed;

        public float speedIncreaseMultiplier;
        public float slopeIncreaseMultiplier;
        
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float slideSpeed = 20f;
        [SerializeField] private float wallRunningSpeed = 8.5f;

        private float desiredMoveSpeed;
        private float lastDesiredMoveSpeed;
        
        [SerializeField] private Transform orientation;

        private float horizontalInput;
        private float verticalInput;

        private Vector3 moveDirection;

        [SerializeField] private Rigidbody rb;

        [SerializeField] private float groundDrag = 5f;

        [SerializeField] private MovementState currentMovementState = MovementState.Walking;

        public enum MovementState
        {
            Walking,
            Sprinting,
            Air,
            Crouching,
            Sliding,
            WallRunning,
            Freeze
        }

        [SerializeField] private bool activeGrapple;
        [SerializeField] private bool frozen;
        [SerializeField] private bool sliding;
        [SerializeField] private bool wallRunning;
        
        [Header("Crouching")] 
        [SerializeField] private float crouchSpeed = 3.5f;
        [SerializeField] private float crouchYScale = 0.5f;
        private float startYScale;
        
        [Header("Jump")] 
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float jumpCooldown = 0.25f;
        [SerializeField] private float airMultiplier = 0.4f;
        [SerializeField] private Transform centerPlayer;
        private bool readyToJump = true;
        
        [Header("Ground Check")] 
        [SerializeField] private float playerHeight = 2f;

        [SerializeField] private LayerMask groundLayer;

        private bool grounded;

        [Header("Slope Handling")] 
        [SerializeField] private float maxSlopeAngle = 40f;
        private RaycastHit slopeHit;
        private bool exitingSlope;

        [Header("Reference")] 
        private CameraController cameraController;

        [Header("Camera Effect")] 
        [SerializeField] private float grappleFov = 95f;

        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool sprintHeld;
        private bool crouchHeld;

        #endregion

        #region Unity Method
        

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsOwner)
            {
                cameraController = CameraController.Instance;
                
                if (cameraController == null)
                {
                    Debug.LogError("[PlayerController] CameraController.Instance is null!");
                }
                CameraController.Instance.FollowTarget(transform,orientation.transform);
                
                inputActions = new PlayerInputActions();
                
                inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
                inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            
                inputActions.Player.Jump.performed += ctx => jumpPressed = true;
                inputActions.Player.Jump.canceled += ctx => jumpPressed = false;
            
                inputActions.Player.Sprint.performed += ctx => sprintHeld = true;
                inputActions.Player.Sprint.canceled += ctx => sprintHeld = false;
            
                inputActions.Player.Crouch.performed += ctx => OnCrouchPressed();
                inputActions.Player.Crouch.canceled += ctx => OnCrouchReleased();
                
                inputActions.Enable();
            }
            
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

        void Start()
        {
            InitComponent();
            
        }
        
        void Update()
        {
            if (!IsOwner) return;

            MyInput();
            SpeedControl();
            StateHandler();
            Drag();
            
            Debug.DrawRay(centerPlayer.position, (Vector3.down * playerHeight * 0.5f) + new Vector3(0, 0.2f, 0), Color.red, 1);
            
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            
            MovePlayer();
        }

        #endregion

        #region Init

        private void InitComponent()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            rb.freezeRotation = true;

            startYScale = transform.localScale.y;
        }

        #endregion

        #region Methods Shift

        private void MyInput()
        {
            horizontalInput = moveInput.x;
            verticalInput = moveInput.y;

            if (jumpPressed && readyToJump && grounded && currentMovementState != MovementState.Crouching)
            {
                readyToJump = false;
                
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        private void OnCrouchPressed()
        {
            crouchHeld = true;
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        private void OnCrouchReleased()
        {
            crouchHeld = false;
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }

        private void MovePlayer()
        {
            if (activeGrapple)
            {
                return;
            }
            
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (OnSlope() && !exitingSlope)
            {
                rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);
                if (rb.linearVelocity.y > 0)
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }
            }
            else if (grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }
            else if (!grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            }

            if (!wallRunning)
            {
                rb.useGravity = !OnSlope();
            }
        }

        private void SpeedControl()
        {
            if (activeGrapple)
            {
                return;
            }
            
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            
            if (OnSlope() && !exitingSlope)
            {
                if (rb.linearVelocity.magnitude > moveSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
                }
            }
            else
            {
                if (flatVel.magnitude > moveSpeed)
                {
                    Vector3 limitedVel = flatVel.normalized * moveSpeed;
                    rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
                }
            }

            if (cameraController != null)
            {
                if (rb.linearVelocity.magnitude > sprintSpeed)
                {
                    if (!cameraController.EffectSpeedActive())
                    {
                        cameraController.ToggleSpeedCameraEffect(true);
                    }
                }
                else
                {
                    cameraController.ToggleSpeedCameraEffect(false);
                }
            }
        }

        public bool IsGrounded() 
        {
            return grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);
        }

        private void Drag()
        {
            if (IsGrounded() && !activeGrapple)
            {
                rb.linearDamping = groundDrag;
            }
            else
            {
                rb.linearDamping = 0;
            }
        }

        private void Jump()
        {
            exitingSlope = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            readyToJump = true;
            exitingSlope = false;
        }

        private void StateHandler()
        {
            if (frozen)
            {
                currentMovementState = MovementState.Freeze;
                desiredMoveSpeed = 0;
                rb.linearVelocity = Vector3.zero;
            }
            else if (wallRunning)
            {
                currentMovementState = MovementState.WallRunning;
                desiredMoveSpeed = wallRunningSpeed;
            }
            else if (sliding)
            {
                currentMovementState = MovementState.Sliding;

                if (OnSlope() && rb.linearVelocity.y < 0.1f)
                    desiredMoveSpeed = slideSpeed;
                else
                    desiredMoveSpeed = sprintSpeed;
            }
            else if (crouchHeld)
            {
                currentMovementState = MovementState.Crouching;
                desiredMoveSpeed = crouchSpeed;
            }
            else if (grounded && sprintHeld)
            {
                currentMovementState = MovementState.Sprinting;
                desiredMoveSpeed = sprintSpeed;
            }
            else if (grounded)
            {
                currentMovementState = MovementState.Walking;
                desiredMoveSpeed = walkSpeed;
            }
            else
            {
                currentMovementState = MovementState.Air;
            }

            if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                moveSpeed = desiredMoveSpeed;
            }

            lastDesiredMoveSpeed = desiredMoveSpeed;
        }

        private IEnumerator SmoothlyLerpMoveSpeed()
        {
            float time = 0;
            float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
            float startValue = moveSpeed;

            while (time < difference)
            {
                moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

                if (OnSlope())
                {
                    float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                    float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                    time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
                }
                else
                {
                    time += Time.deltaTime * speedIncreaseMultiplier;
                }
                yield return null;
            }
            moveSpeed = desiredMoveSpeed;
        }

        private void ResetRestrictions()
        {
            activeGrapple = false;
            if (cameraController != null)
                cameraController.DoFov(80f);
        }
        
        public bool OnSlope()
        { 
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.2f))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }

            return false;
        }

        public Vector3 GetSlopeMoveDirection(Vector3 direction)
        {
            return Vector3.ProjectOnPlane(direction, slopeHit.normal);
        }
        
        #endregion

        #region UI

        private void DisplayText(TMP_Text _text, string _content)
        {
            _text.text = _content;
        }

        #endregion

        #region Utility

        public void SetterBoolSliding(bool _result) => sliding = _result;
        public void SetterBoolWallRunning(bool _result) => wallRunning = _result;
        public void SetterBoolFreezing(bool _result) => frozen = _result;
        
        public bool GetSliding() => sliding;
        public bool GetWallRunning() => wallRunning;
        public bool GetFreezingState() => frozen;

        public MovementState GetMovementState() => currentMovementState;
        public Transform GetOrientation() => orientation;
        public Rigidbody GetPlayerRigidbody() => rb;
        
        #endregion
    }
}