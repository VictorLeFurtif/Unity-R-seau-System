using System;
using System.Collections;
using Enum;
using EventBus;
using Fps_Handle.Scripts.Controller;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        private Rigidbody rb;
        private Collider playerCollider;

        [SerializeField] private LayerMask defaultLayer;
        [SerializeField] private LayerMask layerMaskXRay;
        
        
        private int layerXray;
        private int layerDefault;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            
            if (playerCollider == null)
            {
                playerCollider = GetComponentInChildren<Collider>();
            }
            
            layerXray = LayerMask.NameToLayer("Hider");
            layerDefault = LayerMask.NameToLayer("Default");
        }

        private void Start()
        {
            InitComponent();
        }

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                DebugTp();
            }
            
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
            }
        }

        private IEnumerator TeleportToPosition(Vector3 targetPosition)
        {
            DisablePhysicsRpc();
    
            yield return new WaitForFixedUpdate();
    
            //transform.position = targetPosition;

            if (IsOwner)
            {
                ntTransform.Teleport(targetPosition, Quaternion.identity, Vector3.one);
                //rb.MovePosition(targetPosition);
            }
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
    
            EnablePhysicsRpc();
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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            playerCollider.enabled = false;
            
            pc.SetterMove(false);
            
        }

        [Rpc(SendTo.Everyone)]
        private void EnablePhysicsRpc()
        {
            rb.isKinematic = false;
            playerCollider.enabled = true;
            pc.SetterMove(true);
            /*
            if (!isSeeker.Value || isImprisoned.Value)
            {
                pc.SetterMove(true);
            }*/
        }

        [Rpc(SendTo.Everyone)]
        private void TeleportToSpawnPointRpc(Vector3 spawnPosition)
        {
            StartCoroutine(TeleportToSpawnAndApplyRestrictions(spawnPosition));
        }

       

        #endregion

        #region Getter Setter

        public bool IsSeeker() => isSeeker.Value;

        public bool IsImprisoned() => isImprisoned.Value;

        #endregion

        #region Observer

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            EventManager.OnLobbyEntered += OnLobby;
            EventManager.OnGameEnded += OnGameEnd;
            EventManager.OnGameStarted += OnGameStart;
            EventManager.OnXray += Xray;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            EventManager.OnLobbyEntered -= OnLobby;
            EventManager.OnGameEnded -= OnGameEnd;
            EventManager.OnGameStarted -= OnGameStart;
            EventManager.OnXray -= Xray;
        }

        #endregion

        #region State Game Behavior

        private void OnLobby()
        {
            if (IsServer)
            {
                isImprisoned.Value = false;
            }
            pc.SetterMove(true);
        }

        private void OnGameEnd()
        {
            pc.SetterMove(false);
        }

        private void OnGameStart()
        {
            if (IsServer)
            {
                Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition();
                TeleportToSpawnPointRpc(spawnPos);
            }
        }

        #endregion
        
        #region Seeker Restriction
        
        private IEnumerator TeleportToSpawnAndApplyRestrictions(Vector3 spawnPosition)
        {
            yield return StartCoroutine(TeleportToPosition(spawnPosition));
    
            if (isSeeker.Value && IsOwner)
            {
                yield return StartCoroutine(RestrictionOnGameStartSeeker());
            }
        }
        
        private IEnumerator RestrictionOnGameStartSeeker()
        {
            Debug.Log("CCCCCCCCC");
            pc.SetterMove(false);
            yield return new WaitForSeconds(10);
            pc.SetterMove(true);
        }

        #endregion

        private void DebugTp()
        {
            if (IsServer)
            {
                TeleportToSpawnPointRpc(new Vector3(-50.7999992f,75.0899963f,-199.199997f));
            }
        }

        private void Xray()
        {
            if (!isSeeker.Value && IsServer)
            {
                ActivateXrayRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ActivateXrayRpc()
        {
            StartCoroutine(XrayCoroutine());
        }

        private IEnumerator XrayCoroutine()
        {
            gameObject.layer = layerXray;
            
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            foreach (var rend in renderers)
            {
                rend.gameObject.layer = layerXray;
            }
            
            
            yield return new WaitForSeconds(5);
            
            gameObject.layer = layerDefault;
            
            foreach (var rend in renderers)
            {
                rend.gameObject.layer = layerDefault;
            }
            
        }
    }
}