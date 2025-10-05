using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fps_Handle.Scripts.Controller
{
    public class CameraController : NetworkBehaviour
    {
        #region Singleton

        public static CameraController Instance { get; private set; }

        #endregion

        #region Variable


        private Transform orientation;
        [SerializeField] private Transform camHolder;

        private float xRotation;
        private float yRotation;

        [FormerlySerializedAs("camera")] [SerializeField] private Camera cameraPlayer;

        [SerializeField] private GameObject cameraSpeedEffect;
        private bool effectSpeed = false;

        private Transform currentTarget; 

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

        private void Start()
        {
            InitCursor();
        }
        

        private void LateUpdate()
        {
            if (currentTarget != null)
            {
                transform.position = currentTarget.position;
            }
        }

        #endregion

        #region Initialization Methods

        private void InitCursor()
        {
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
        
        public void FollowTarget(Transform target, Transform targetOrientation)
        {
            currentTarget = target;
            orientation = targetOrientation;
            
            xRotation = 0;
            yRotation = 0;
            
        }

        #endregion

        #region Mouse Control

        public void MouseController(Vector2 lookInput, float sensX, float sensY)
        {
            float mouseX = lookInput.x * Time.deltaTime * sensX;
            float mouseY = lookInput.y * Time.deltaTime * sensY;
            
            yRotation += mouseX;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90, 90);
            
            if (camHolder != null)
                camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            
            if (orientation != null)
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        }

        public void DoFov(float endValue)
        {
            if (cameraPlayer != null)
                cameraPlayer.DOFieldOfView(endValue, 0.25f);
        }

        public void DoTile(float zTilt)
        {
            transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
        }

        public void ToggleSpeedCameraEffect(bool condition)
        {
            if (cameraSpeedEffect != null)
            {
                cameraSpeedEffect.SetActive(condition);
                effectSpeed = condition;
            }
        }

        public bool EffectSpeedActive()
        {
            return effectSpeed;
        }
        
        #endregion
    }
}