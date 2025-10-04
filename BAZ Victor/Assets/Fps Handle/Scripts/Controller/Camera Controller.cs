using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fps_Handle.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        #region Variable

        [Header("Parameters")]
        
        [SerializeField] private float sensX;
        [SerializeField] private float sensY;

        [SerializeField] private Transform orientation;
        [SerializeField] private Transform camHolder;

        private float xRotation;
        private float yRotation;

        [FormerlySerializedAs("camera")] [SerializeField] private Camera cameraPlayer;

        [SerializeField] private GameObject cameraSpeedEffect;
        private bool effectSpeed = false;

        private PlayerInputActions inputActions;
        private Vector2 lookInput;

        #endregion

        #region Unity Methods

        private void Awake() 
        {
            inputActions = new PlayerInputActions();
        }

        private void OnEnable() 
        {
            inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
            inputActions.Enable();
        }

        private void OnDisable() 
        {
            inputActions.Disable();
        }

        private void Start()
        {
            InitCursor();
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            MouseController();
        }

        #endregion

        #region Initi Methods

        private void InitCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        #endregion

        #region Mouse Control

        private void MouseController()
        {
            float mouseX = lookInput.x * Time.deltaTime * sensX;
            float mouseY = lookInput.y * Time.deltaTime * sensY;
            
            yRotation += mouseX;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90, 90);
            
            camHolder.rotation = Quaternion.Euler(xRotation,yRotation,0);
            orientation.rotation = Quaternion.Euler(0,yRotation,0);
        }

        public void DoFov(float endValue)
        {
            cameraPlayer.DOFieldOfView(endValue, 0.25f);
        }

        public void DoTile(float zTilt)
        {
            transform.DOLocalRotate(new Vector3(0, 0, zTilt),0.25f);
        }

        public void ToggleSpeedCameraEffect(bool condition)
        {
            cameraSpeedEffect.SetActive(condition);
            effectSpeed = condition;
        }

        public bool EffectSpeedActive()
        {
            return effectSpeed;
        }
        
        #endregion
    }
}