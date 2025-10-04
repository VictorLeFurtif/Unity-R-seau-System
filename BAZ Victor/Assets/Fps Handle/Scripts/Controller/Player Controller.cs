using System.Collections;
using Data_Script;
using TMPro;
using UnityEngine;

namespace Fps_Handle.Scripts.Controller
{
    public class PlayerController : MonoBehaviour
    {
        #region Variable

        [Header("UI")] 
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text stateText;
        
        [Header("Data")]
        [SerializeField] private PlayerData tempoData;
        [SerializeField] private PlayerDataInstance finalData;
        
        [Header("Movement")] 
        private float moveSpeed;

        public float speedIncreaseMultiplier;
        public float slopeIncreaseMultiplier;
        
        [SerializeField] private float slideSpeed;
        [SerializeField] private float wallRunningSpeed;

        private float desiredMoveSpeed;
        private float lastDesiredMoveSpeed;
        
        [SerializeField] private Transform orientation;

        private float horizontalInput;
        private float verticalInput;

        private Vector3 moveDirection;

        [SerializeField] private Rigidbody rb;

        [SerializeField] private float groundDrag;

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
        [SerializeField] private float crouchSpeed;
        [SerializeField] private float crouchYScale;
        private float startYScale;
        
        [Header("Jump")] 
        [SerializeField] private float jumpForce;
        [SerializeField] private float jumpCooldown;
        [SerializeField] private float airMultiplier;
        [SerializeField] private Transform centerPlayer;
        private bool readyToJump = true;
        
        [Header("Ground Check")] 
        [SerializeField] private float playerHeight;

        [SerializeField] private LayerMask groundLayer;

        private bool grounded;
        
        [Header("Slope Handling")] 
        [SerializeField] private float maxSlopeAngle;
        private RaycastHit slopeHit;
        private bool exitingSlope;

        [Header("Reference")] 
        [SerializeField] private CameraController cameraController;


        [Header("Camera Effect")] [SerializeField]
        private float grappleFov = 95;

        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool sprintHeld;
        private bool crouchHeld;

        #endregion

        #region Unity Method

        private void Awake() 
        {
            inputActions = new PlayerInputActions();
        }

        private void OnEnable() 
        {
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

        private void OnDisable() 
        {
            inputActions.Disable();
        }

        void Start()
        {
            InitComponent();
            InitData();
        }
        
        void Update()
        {
            MyInput();
            SpeedControl();
            StateHandler();
            Drag();
            Debug.DrawRay(centerPlayer.position, (Vector3.down * playerHeight * 0.5f) + new Vector3(0,0.2f,0), Color.red,1);
            DisplayText(speedText, $"Speed : {rb.linearVelocity.magnitude:00.00}");
            DisplayText(stateText, $"State : {currentMovementState.ToString()}");
        }

        private void FixedUpdate()
        {
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

        private void InitData()
        {
            finalData = tempoData.Instance();
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
                Invoke(nameof(ResetJump),jumpCooldown);
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
                if (rb.linearVelocity.y > 0 )
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }
            }
            
            else if (grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f,ForceMode.Force);
            }
            else if (!grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier,ForceMode.Force);
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
            
            if (OnSlope()&& !exitingSlope)
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

            if (rb.linearVelocity.magnitude > finalData.sprintSpeed )
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

        public bool IsGrounded() 
        {
            return grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);
        }

        

        private void Drag()
        {
            if (IsGrounded() && ! activeGrapple)
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
                    desiredMoveSpeed = finalData.sprintSpeed;
            }

            else if (crouchHeld)
            {
                currentMovementState = MovementState.Crouching;
                desiredMoveSpeed = crouchSpeed;
            }

            else if(grounded && sprintHeld)
            {
                currentMovementState = MovementState.Sprinting;
                desiredMoveSpeed = finalData.sprintSpeed;
            }

            else if (grounded)
            {
                currentMovementState = MovementState.Walking;
                desiredMoveSpeed = finalData.walkSpeed;
            }

            else
            {
                currentMovementState = MovementState.Air;
            }

            if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
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

        private Vector3 velocityToSet;

        private bool enableMovementOnNextTouch;
        public void SetVelocity()
        {
            cameraController.DoFov(grappleFov);
            enableMovementOnNextTouch = true;
            rb.linearVelocity = velocityToSet;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (enableMovementOnNextTouch)
            {
                enableMovementOnNextTouch = false;
                ResetRestrictions();
            }
        }

        private void ResetRestrictions()
        {
            activeGrapple = false;
            cameraController.DoFov(80f);
        }
        
        public bool OnSlope()
        { 
            if (Physics.Raycast(transform.position,Vector3.down,out slopeHit,playerHeight * 0.5f + 0.2f))
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
        
        private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
        {
            float gravity = Physics.gravity.y;
            float displacementY = endPoint.y - startPoint.y;
            Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);
            
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
            Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

            return velocityXZ + velocityY;
        }

        
        public void JumpToPoint(Vector3 targetPosition, float trajectoryHeight)
        {
            SetterBoolActiveGrappling(true);
            SetVectorVelocityToSet(CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight));
            Invoke(nameof(SetVelocity),0.1f);
            Invoke(nameof(ResetRestrictions),2f);
        }
        
        #endregion

        #region UI

        private void DisplayText(TMP_Text _text, string _content)
        {
            _text.text = _content;
        }

        #endregion

        #region Utility

        private bool GetterInfo(bool _boolean)
        {
            return _boolean;
        }

        private Transform GetterInfo(Transform _transform)
        {
            return _transform;
        }

        private MovementState GetterInfo(MovementState state)
        {
            return state;
        }
        
        private void SetterBool(ref bool _objectif,bool result)
        {
            _objectif = result;
        }

        public void SetterBoolSliding(bool _result) => SetterBool(ref sliding, _result);
        public void SetterBoolWallRunning(bool _result) => SetterBool(ref wallRunning, _result);

        public void SetterBoolFreezing(bool _result) => SetterBool(ref frozen, _result);
        public void SetterBoolActiveGrappling(bool _result) => SetterBool(ref activeGrapple, _result);
        
        public bool GetSliding() => GetterInfo(sliding);
        public bool GetWallRunning() => GetterInfo(wallRunning);

        public bool GetFreezingState() => GetterInfo(frozen);

        public MovementState GetMovementState() => GetterInfo(currentMovementState);

        public Transform GetOrientation() => GetterInfo(orientation);

        private Rigidbody GetRigidbody(ref Rigidbody _rigidbody)
        {
            return _rigidbody;
        }

        public Rigidbody GetPlayerRigidbody() => GetRigidbody(ref rb);

        public void SetVectorVelocityToSet(Vector3 _vector)
        {
            velocityToSet = _vector;
        }
        
        #endregion
    }
}