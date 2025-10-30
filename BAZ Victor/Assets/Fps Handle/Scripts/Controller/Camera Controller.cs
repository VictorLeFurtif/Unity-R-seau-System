using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Fps_Handle.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        #region Singleton

        private static CameraController instance;

        public static CameraController Instance => instance;

        #endregion
        
        #region Variable
        
        [SerializeField] private Camera cameraPlayer;
        [SerializeField] private GameObject cameraSpeedEffect;
        
        private bool isEffectSpeed = false;
        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _tweenerRotationZCamera;
        private TweenerCore<float, float, FloatOptions> _tweenerFovCamera;
        
        [SerializeField] private Transform cameraHolder;
        
        //
        
        float yaw;
        float pitch;
        float horizontalInput;
        float verticalInput;
        
        [Header("Rotation")]
       
        [SerializeField] float verticalLimit = 80f;
        [SerializeField] float followSmooth  = 15f;

        private PlayerController pc;

        private Transform cameraTarget;
        
        
        #endregion
        
        #region Unity Methods

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            
        }

        #endregion

        #region Initialization

        private void Start()
        {
            ToggleSpeedCameraEffect(false);
        }

        public void Initialize(Transform target)
        {
            /*xRotation = 0;
            yRotation = 0;*/
            cameraTarget = target;
        }
        

        #endregion

        #region Mouse Control
        
        
        public void InputMouse(Vector2 lookInput, float sensX, float sensY) 
        {
            horizontalInput = lookInput.x;
            verticalInput   = lookInput.y;

            float mouseX = lookInput.x * sensX;
            float mouseY = lookInput.y * sensY;

            yaw   += mouseX;
            pitch -= mouseY;
            pitch =  Mathf.Clamp(pitch, -verticalLimit, verticalLimit);
            
        }
        
        
        public void MouseController(Transform orientation)
        {
		orientation.rotation = Quaternion.Euler(0f, yaw, 0f);

        cameraHolder.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        cameraHolder.transform.position = Vector3.Lerp(
            cameraHolder.transform.position,
			cameraTarget.position,
			Time.deltaTime * followSmooth
		);
        }
        
        public void DoFov(float endValue,float time)
        {
            _tweenerFovCamera.Kill();
            if (cameraPlayer != null)
                _tweenerFovCamera = cameraPlayer.DOFieldOfView(endValue, time);
        }

        public void DoTile(float zTilt)
        {
            _tweenerRotationZCamera.Kill();
            _tweenerRotationZCamera = transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.5f);
        }

        public void ToggleSpeedCameraEffect(bool active)
        {
            if (cameraSpeedEffect != null)
            {
                cameraSpeedEffect.SetActive(active);
                isEffectSpeed = active;
            }
        }

        public bool EffectSpeedActive()
        {
            return isEffectSpeed;
        }
        
        #endregion
    }
}