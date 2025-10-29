using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Fps_Handle.Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        #region Variable

        [SerializeField] private Transform camHolder;
        [SerializeField] private Camera cameraPlayer;
        [SerializeField] private GameObject cameraSpeedEffect;
        
        private float xRotation;
        private float yRotation;
        private bool isEffectSpeed = false;
        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _tweenerRotationZCamera;
        private TweenerCore<float, float, FloatOptions> _tweenerFovCamera;

        #endregion

        #region Initialization

        private void Start()
        {
            ToggleSpeedCameraEffect(false);
        }

        public void Initialize()
        {
            xRotation = 0;
            yRotation = 0;
        }

        public void SetCameraActive(bool isOwner)
        {
            if (cameraPlayer != null)
            {
                cameraPlayer.enabled = isOwner;
                AudioListener listener = cameraPlayer.GetComponent<AudioListener>();
                if (listener != null)
                    listener.enabled = isOwner;
            }
            
            Debug.Log($"[CameraController] Camera enabled: {isOwner}");
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
        }

        public void RotateOrientation(Transform orientation)
        {
            if (orientation != null)
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
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