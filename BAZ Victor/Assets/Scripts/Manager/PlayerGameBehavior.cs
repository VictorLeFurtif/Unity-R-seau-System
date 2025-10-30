using System;
using System.Collections;
using Enum;
using Fps_Handle.Scripts.Controller;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Manager
{
    public class PlayerGameBehavior : NetworkBehaviour
    {
        #region Fields

        [SerializeField] private NetworkVariable<bool> isSeeker = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>(false);
        
        private GameObject prisonGameObject;
        
        private Vector3 prisonMin;
        private Vector3 prisonMax;

        private PlayerController pc;

        [SerializeField] private NetworkTransform ntTransform;
        
        // AJOUT: Référence au Rigidbody et Collider
        private Rigidbody rb;
        private Collider playerCollider;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            
            // Si le collider est sur un enfant
            if (playerCollider == null)
            {
                playerCollider = GetComponentInChildren<Collider>();
            }
        }

        private void Start()
        {
            InitComponent();
        }

        private void Update()
        {
            MovementInPrison();
        }

        #endregion

        #region Init

        private void InitComponent()
        {
            prisonGameObject = GameObject.FindWithTag("Prison");
            
            if (prisonGameObject == null)
            {
                Debug.LogError("PRISON PAS LAAAAAA");
            }
            
            BoxCollider col = prisonGameObject.GetComponent<BoxCollider>();
            
            if (col == null)
            {
                Debug.LogError("COLLIDER PAS LAAAAAA");
            }
            
            prisonMin = col.bounds.min;
            prisonMax = col.bounds.max;
        }

        #endregion
        
        #region State Methods
        
        public void SetImprisoned(bool value)
        {
            SetImprisonedRpc(value);
        }
        
        private void OnCapturePrison()
        {
            StartCoroutine(TeleportPrisonIe());
        }

        private void OnReleasePrison()
        {
            if (IsOwner)
            {
                //pc.SetterMove(true);
            }
        }

        #endregion

        #region Shift Restriction

        private void MovementInPrison()
        {
            if (!IsOwner || isSeeker.Value || !IsImprisoned()) return;
            
            Vector3 newPos = transform.position;
                
            newPos.x = Mathf.Clamp(newPos.x, prisonMin.x, prisonMax.x);
            newPos.z = Mathf.Clamp(newPos.z, prisonMin.z, prisonMax.z);
            gameObject.transform.localPosition = newPos;
        }

        #endregion

        #region Teleport In Prison

        private IEnumerator TeleportPrisonIe()
        {
            DisablePhysicsRpc();
            
            yield return new WaitForFixedUpdate();
            
            Vector3 prisonPos = prisonGameObject.transform.position + Vector3.up * 2f;
            transform.position = prisonPos;

            if (IsOwner)
            {
                ntTransform.Teleport(prisonPos, Quaternion.identity, Vector3.one);
            }
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            
            EnablePhysicsRpc();

            if (this != null && prisonGameObject != null)
            {
                PrisonZone prisonZone = prisonGameObject.GetComponent<PrisonZone>();
                if (prisonZone != null)
                    prisonZone.AddPrisoner(this);
                
                GameManager.Instance.ToggleTimerHider(GameManager.Instance.CheckIfSeekerWon());
                
                yield return new WaitForFixedUpdate();
                
                GameManager.Instance.CheckIfEndGame();
            }
        }
        
        #endregion

        #region RPC

        [Rpc(SendTo.Everyone)]
        private void SetImprisonedRpc(bool value)
        {
            if (IsServer)
            {
                isImprisoned.Value = value;
            }
            
            if (value)
            {   
                OnCapturePrison();
            }
            else
            {
                OnReleasePrison();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void DisablePhysicsRpc()
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            playerCollider.enabled = false;
            
            pc.SetterMove(false);
            
        }

        [Rpc(SendTo.Everyone)]
        private void EnablePhysicsRpc()
        {
            rb.isKinematic = false;
            playerCollider.enabled = true;
            pc.SetterMove(true);
            
        }

        #endregion

        #region Getter Setter

        public bool IsSeeker() => isSeeker.Value;

        public bool IsImprisoned() => isImprisoned.Value;

        #endregion
    }
}