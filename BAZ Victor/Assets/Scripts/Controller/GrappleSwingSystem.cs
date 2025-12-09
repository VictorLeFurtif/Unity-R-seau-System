using System;
using Fps_Handle.Scripts.Controller;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Controller
{
    public class GrappleSwingSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private LineRenderer rope;
        [SerializeField] private Transform gunTip;
        private Transform cam;
        [SerializeField] private Transform player;
        [SerializeField] private LayerMask grappleLayer;

        [Header("Swing Settings")]
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float spring = 4.5f;
        [SerializeField] private float damper = 7f;
        [SerializeField] private float massScale = 4.5f;

        [Header("Movement Forces")]
        [SerializeField] private Transform orientation;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float horizontalThrust = 3000f;
        [SerializeField] private float forwardThrust = 5000f;
        [SerializeField] private float extendSpeed = 8f;

        [Header("Prediction")]
        [SerializeField] private float predictionRadius = 1f;

        #endregion

        #region Private

        private PlayerController controller;
        private SpringJoint joint;
        private Vector3 swingPoint;
        private Vector3 ropeCurrentPos;

        private bool grapplePressed;

        private RaycastHit lastHit;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            if (cam == null)
            {
                cam = CameraController.Instance.CameraTransform();
            }
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!controller.IsOwner) return;

            if (!controller.CanUseGrapple) return;
            
            if (grapplePressed && joint == null)
                TryStartGrapple();

            if (!grapplePressed && joint != null)
                StopGrapple();

            PredictPoint();

            if (joint != null)
                ApplySwingMovement();
        }

        private void LateUpdate()
        {
            DrawRope();
        }

        #endregion

        public void SetGrapplePressed(bool value)
        {
            grapplePressed = value;
        }

        #region Grapple Logic

        private void TryStartGrapple()
        {
            if (lastHit.point == Vector3.zero)
                return;

            controller.ResetRestrictions();
            controller.SetterMove(true);

            swingPoint = lastHit.point;

            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = swingPoint;

            float distance = Vector3.Distance(player.position, swingPoint);
            joint.maxDistance = distance * 0.8f;
            joint.minDistance = distance * 0.25f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            rope.positionCount = 2;
            ropeCurrentPos = gunTip.position;

            controller.SetActiveGrapple(true);
        }

        public void StopGrapple()
        {
            controller.SetActiveGrapple(false);

            if (joint != null)
                Destroy(joint);

            rope.positionCount = 0;
        }

        #endregion

        #region Prediction

        private void PredictPoint()
        {
            if (joint != null) return;

            RaycastHit hit;
            if (Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, grappleLayer))
            {
                lastHit = hit;
            }
            else
            {
                lastHit = new RaycastHit(); 
            }
        }


        #endregion

        #region Swing Movement

        private void ApplySwingMovement()
        {
            //équivalent old input system mais genre dans le nouveau
            if (Keyboard.current.aKey.isPressed)
                rb.AddForce(-orientation.right * horizontalThrust * Time.deltaTime);

            if (Keyboard.current.dKey.isPressed)
                rb.AddForce(orientation.right * horizontalThrust * Time.deltaTime);

            if (Keyboard.current.wKey.isPressed)
                rb.AddForce(orientation.forward * horizontalThrust * Time.deltaTime);

            if (Keyboard.current.spaceKey.isPressed)
            {
                Vector3 dir = (swingPoint - transform.position).normalized;
                rb.AddForce(dir * forwardThrust * Time.deltaTime);

                float dist = Vector3.Distance(transform.position, swingPoint);
                joint.maxDistance = dist * 0.8f;
                joint.minDistance = dist * 0.25f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                float dist = Vector3.Distance(transform.position, swingPoint) + extendSpeed;
                joint.maxDistance = dist * 0.8f;
                joint.minDistance = dist * 0.25f;
            }
        }

        #endregion

        #region Rope

        private void DrawRope()
        {
            if (joint == null) return;

            ropeCurrentPos =
                Vector3.Lerp(ropeCurrentPos, swingPoint, Time.deltaTime * 8f);

            rope.SetPosition(0, gunTip.position);
            rope.SetPosition(1, ropeCurrentPos);
        }

        #endregion
    }
}
