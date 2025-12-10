using System;
using System.Collections;
using Data.Scripts;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fps_Handle.Scripts.Controller
{
    public class PlayerController : NetworkBehaviour
    {
        #region Variable

        private bool canMove = true;
        
        [SerializeField] private PlayerControllerData data;
        
        private float moveSpeed;
        private float desiredMoveSpeed;
        private float lastDesiredMoveSpeed;
        

        private float horizontalInput;
        private float verticalInput;

        private Vector3 moveDirection;

        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider colliderPlayer;

        private MovementState currentMovementState = MovementState.Walking;

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

        private bool activeGrapple;
        private bool frozen;
        private bool sliding;
        private bool wallRunning;
        
        private float startYScale;
        
        [SerializeField] private Transform centerPlayer;
        private bool readyToJump = true;

        private bool grounded;
        private bool canSlide;
        
        private RaycastHit slopeHit;
        private bool exitingSlope;
        
        private CameraController cameraController;

        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool sprintHeld;
        private bool crouchHeld;
        private Vector2 lookInput;

        [SerializeField] private Transform cameraPosition;

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshRenderer meshRendererFace;
        
        private GrappleSwingSystem grappleSystem;
        [SerializeField] private bool canUseGrapple = true;

        #endregion

        #region Unity Method

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
    
            if (cameraController == null)
            {
                cameraController = CameraController.Instance;
            }
    
            if (IsOwner)
            {
                ToggleCursor(true);

                meshRenderer.enabled = false;
                meshRendererFace.enabled = false;
                
                if (cameraController == null)
                {
                    Debug.LogError("[PlayerController] CameraController not found in children!");
                }
                else
                {
                    cameraController.Initialize(cameraPosition);
                }
        
                inputActions = new PlayerInputActions();
        
                inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
                inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        
                inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
                inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    
                inputActions.Player.Jump.performed += ctx => jumpPressed = true;
                inputActions.Player.Jump.canceled += ctx => jumpPressed = false;
    
                inputActions.Player.Sprint.performed += ctx => sprintHeld = true;
                inputActions.Player.Sprint.canceled += ctx => sprintHeld = false;
    
                inputActions.Player.Crouch.performed += ctx => OnCrouchPressed();
                inputActions.Player.Crouch.canceled += ctx => OnCrouchReleased();

                if (canUseGrapple)
                {
                    grappleSystem = GetComponent<GrappleSwingSystem>();

                    inputActions.Player.Grapple.performed += ctx => grappleSystem.SetGrapplePressed(true);
                    inputActions.Player.Grapple.canceled += ctx => grappleSystem.SetGrapplePressed(false);
                }
                
                
        
                inputActions.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
    
            StopAllCoroutines();
            
            if (IsOwner && inputActions != null)
            {
                ToggleCursor(false);
                
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

            if (cameraController != null)
                cameraController.InputMouse(lookInput, data.SensX,data.SensY);

            if (!canMove) return;
            
            MyInput();
            SpeedControl();
            StateHandler();
            Drag();
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            if (cameraController != null)
                cameraController.MouseController(transform);
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

            if (colliderPlayer == null)
            {
                colliderPlayer = GetComponentInChildren<Collider>();
            }

            rb.freezeRotation = true;

            startYScale = transform.localScale.y;
        }
        
        private void ToggleCursor(bool playerSpawning)
        {
            Cursor.lockState = playerSpawning ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playerSpawning;
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
                jumpPressed = false;
                Jump();
                Invoke(nameof(ResetJump), data.JumpCooldown);
            }
        }

        private void OnCrouchPressed()
        {
            crouchHeld = true;
            transform.localScale = new Vector3(transform.localScale.x, data.CrouchYScale, transform.localScale.z);
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
            
            moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

            if (OnSlope() && !exitingSlope) 
            {
                rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 10f, ForceMode.Force);
                rb.useGravity = rb.linearVelocity.y > 0;
            }
            else if (grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 9f , ForceMode.Force);
            }
            else if (!grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * data.AirMultiplier * 9f, ForceMode.Force);
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
                if (rb.linearVelocity.magnitude > data.SprintSpeed)
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
            return grounded = Physics.Raycast(transform.position, Vector3.down
                , data.PlayerHeight * 0.5f + 0.2f, data.GroundLayer);
        }
        
        public bool CanSlide() 
        {
            return canSlide = Physics.Raycast(transform.position, Vector3.down
                , data.PlayerHeight +3f, data.GroundLayer);
        }

        private void Drag()
        {
            if (IsGrounded() && !activeGrapple)
            {
                rb.linearDamping = data.GroundDrag;
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
            rb.AddForce(transform.up * data.JumpForce, ForceMode.Impulse);
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
                desiredMoveSpeed = data.WallRunningSpeed;
            }
            else if (sliding)
            {
                currentMovementState = MovementState.Sliding;

                if (OnSlope() && rb.linearVelocity.y < 0.1f)
                    desiredMoveSpeed = data.SlideSpeed;
                else
                    desiredMoveSpeed = data.SprintSpeed;
            }
            else if (crouchHeld)
            {
                currentMovementState = MovementState.Crouching;
                desiredMoveSpeed = data.CrouchSpeed;
            }
            else if (grounded && sprintHeld)
            {
                currentMovementState = MovementState.Sprinting;
                desiredMoveSpeed = data.SprintSpeed;
            }
            else if (grounded)
            {
                currentMovementState = MovementState.Walking;
                desiredMoveSpeed = data.WalkSpeed;
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

                    time += Time.deltaTime * data.SpeedIncreaseMultiplier * data.SlopeIncreaseMultiplier * slopeAngleIncrease;
                }
                else
                {
                    time += Time.deltaTime * data.SpeedIncreaseMultiplier;
                }
                yield return null;
            }
            moveSpeed = desiredMoveSpeed;
        }

        public void ResetRestrictions()
        {
            activeGrapple = false;
        }
        
        public bool OnSlope()
        { 
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, data.PlayerHeight * 0.5f + 0.2f))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < data.MaxSlopeAngle && angle != 0;
            }

            return false;
        }

        public Vector3 GetSlopeMoveDirection(Vector3 direction)
        {
            return Vector3.ProjectOnPlane(direction, slopeHit.normal);
        }

        public void ResetVelocity()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        #endregion

        #region Utility
        public bool CanUseGrapple => canUseGrapple;

        public void SetterMove(bool value)
        {
            canMove = value;
        }

        public void SetterBoolSliding(bool _result) => sliding = _result;
        public void SetterBoolWallRunning(bool _result) => wallRunning = _result;
        public bool GetSliding() => sliding;
        public bool GetWallRunning() => wallRunning;
        public MovementState GetMovementState() => currentMovementState;
    
        public Rigidbody GetPlayerRigidbody() => rb;

        public void SetterCollider(bool _result) => colliderPlayer.enabled = _result;
        
        public void SetActiveGrapple(bool value)
        {
            activeGrapple = value;
        }


        #endregion
    }
}