using UnityEngine;

namespace Fps_Handle.Scripts.Controller
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraPosition;

        private void Start()
        {
            InitCameraTransform();
        }

        private void Update()
        {
            CameraToPlayer();
        }

        private void CameraToPlayer()
        {
            transform.position = cameraPosition.position;
        }

        private void InitCameraTransform()
        {
            if (cameraPosition != null) return;
            cameraPosition = GetComponentInChildren<Transform>();
            Debug.LogError("No Transform found for the camera position");
        }
    }
}
