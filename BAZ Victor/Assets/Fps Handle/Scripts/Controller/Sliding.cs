using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Fps_Handle.Scripts.Controller
{
    public class Sliding : NetworkBehaviour
    {
        #region Fields

        [Header("References")]
        [SerializeField] private Transform orientation;
        [SerializeField] private Transform playerObj;

        private Rigidbody rb;
        private PlayerController pc;

        [Header("Sliding")] 
        [SerializeField] private float maxSlideTime = 1.5f;
        [SerializeField] private float slideForce = 200f;
        private float slideTimer;
        
        [SerializeField] private float slideYScale = 0.5f;
        private float startYScale;

        private float horizontalInput;
        private float verticalInput;
        
        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private bool slidePressed;
        
        #endregion

        #region Unity Methods

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner) return;

            inputActions = new PlayerInputActions();
            
            inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            
            inputActions.Player.Slide.performed += ctx => OnSlidePressed();
            inputActions.Player.Slide.canceled += ctx => OnSlideReleased();
            
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
            if (!IsOwner) return;

            InputMovement();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (pc.GetSliding())
            {
                SlidingMovement();
            }
        }

        #endregion

        #region Init

        private void InitComponent()
        {
            rb = GetComponent<Rigidbody>();
            pc = GetComponent<PlayerController>();
            startYScale = playerObj.localScale.y;
        }

        #endregion

        #region Slide Methods
        
        private void InputMovement()
        {
            horizontalInput = moveInput.x;
            verticalInput = moveInput.y;
        }

        private void OnSlidePressed()
        {
            slidePressed = true;
            if ((horizontalInput != 0 || verticalInput != 0) && !pc.GetWallRunning())
            {
                StartSlide();
            }
        }

        private void OnSlideReleased()
        {
            slidePressed = false;
            if (pc.GetSliding())
            {
                StopSlide();
            }
        }
        
        private void StartSlide()
        {
            pc.SetterBoolSliding(true);
            
            playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            slideTimer = maxSlideTime;

            if (IsOwner)
            {
                StartSlideRpc();
            }
        }

        private void SlidingMovement()
        {
            Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (!pc.OnSlope() || rb.linearVelocity.y > -0.1f)
            {
                rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

                slideTimer -= Time.deltaTime;
            }
            else
            {
                rb.AddForce(pc.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
            }
            
            if (slideTimer <= 0)
            {
                StopSlide();
            }
        }
        
        private void StopSlide()
        {
            pc.SetterBoolSliding(false);
            
            playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);

            if (IsOwner)
            {
                StopSlideRpc();
            }
        }

        #endregion

        #region Network RPCs 

        [Rpc(SendTo.NotMe)]
        private void StartSlideRpc()
        {
            playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        }

        [Rpc(SendTo.NotMe)]
        private void StopSlideRpc()
        {
            playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
        }

        #endregion
    }
}