using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Fps_Handle.Scripts.Controller;

public class GrappleSwingSystem : NetworkBehaviour
{
    #region Vars
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

    private PlayerController controller;
    private SpringJoint joint;
    private Vector3 swingPoint;
    private Vector3 ropeCurrentPos;
    private bool grapplePressed;
    private RaycastHit lastHit;
    #endregion

    private void OnEnable()
    {
        cam = CameraController.Instance.CameraTransform();
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!IsOwner) return;
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

    public void SetGrapplePressed(bool value)
    {
        grapplePressed = value;
    }

    private void TryStartGrapple()
    {
        if (lastHit.point == Vector3.zero) return;

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

        StartRopeClientRpc(swingPoint);
    }

    [ClientRpc]
    private void StartRopeClientRpc(Vector3 point)
    {
        rope.positionCount = 2;
        swingPoint = point;
        ropeCurrentPos = gunTip.position;
    }

    private void StopGrapple()
    {
        controller.SetActiveGrapple(false);

        if (joint != null)
            Destroy(joint);

        rope.positionCount = 0;

        StopRopeClientRpc();
    }

    [ClientRpc]
    private void StopRopeClientRpc()
    {
        rope.positionCount = 0;
    }

    private void PredictPoint()
    {
        if (joint != null) return;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxDistance, grappleLayer))
            lastHit = hit;
        else
            lastHit = new RaycastHit();
    }

    private void ApplySwingMovement()
    {
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
        }

        if (Keyboard.current.sKey.isPressed)
        {
            float dist = Vector3.Distance(transform.position, swingPoint) + extendSpeed;
            joint.maxDistance = dist * 0.8f;
            joint.minDistance = dist * 0.25f;
        }
    }

    private void DrawRope()
    {
        if (rope.positionCount == 0) return;

        ropeCurrentPos = Vector3.Lerp(ropeCurrentPos, swingPoint, Time.deltaTime * 8f);
        rope.SetPosition(0, gunTip.position);
        rope.SetPosition(1, ropeCurrentPos);
    }
}
