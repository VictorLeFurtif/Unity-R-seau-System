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
        private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>
            (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
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

        private bool isTeleporting = false;

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
                return;
            }
            
            BoxCollider col = prisonGameObject.GetComponent<BoxCollider>();
            
            if (col == null)
            {
                Debug.LogError("COLLIDER PAS LAAAAAA");
                return;
            }
            
            prisonMin.x = col.bounds.min.x;
            prisonMin.z = col.bounds.min.z;
            prisonMin.y = col.bounds.min.y;
            prisonMax.y = col.bounds.max.y + 5;
            prisonMax.x = col.bounds.max.x;
            prisonMax.z = col.bounds.max.z;
        }

        #endregion
        
        #region State Methods
        
        public void SetImprisoned(bool value)
        {
            if (!IsServer) return;
            
            isImprisoned.Value = value;
            
            if (value)
            {
                TeleportToPrisonServerRpc();
            }
            else
            {
                OnReleasePrisonClientRpc();
            }
        }

        private void OnReleasePrison()
        {
            
        }

        #endregion

        #region Shift Restriction

        private void MovementInPrison()
        {
            if (!IsOwner || isSeeker.Value || !IsImprisoned() || isTeleporting) return;
            
            Vector3 newPos = transform.position;
                
            newPos.x = Mathf.Clamp(newPos.x, prisonMin.x, prisonMax.x);
            newPos.y = Mathf.Clamp(newPos.y, prisonMin.y, prisonMax.y);
            newPos.z = Mathf.Clamp(newPos.z, prisonMin.z, prisonMax.z);
            
            transform.position = newPos;
        }

        #endregion

        #region Teleport In Prison

        [Rpc(SendTo.Server)]
        private void TeleportToPrisonServerRpc()
        {
            if (prisonGameObject != null)
            {
                PrisonZone prisonZone = prisonGameObject.GetComponent<PrisonZone>();
                if (prisonZone != null)
                    prisonZone.AddPrisoner(this);
            }

            Vector3 prisonPos = prisonGameObject.transform.position + Vector3.up * 2f;
            TeleportToPositionClientRpc(prisonPos, true);
        }

        [Rpc(SendTo.Owner)]
        private void TeleportToPositionClientRpc(Vector3 targetPosition, bool toPrison)
        {
            if (!IsOwner) return;
            StartCoroutine(TeleportToPositionCoroutine(targetPosition, toPrison));
        }

        private IEnumerator TeleportToPositionCoroutine(Vector3 targetPosition, bool toPrison)
        {
            isTeleporting = true;
            pc.enabled = false;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            playerCollider.enabled = false;
            
            yield return new WaitForFixedUpdate();

            if (ntTransform != null)
            {
                ntTransform.Teleport(targetPosition, Quaternion.identity, Vector3.one);
            }
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            
            rb.isKinematic = false;
            playerCollider.enabled = true;
            pc.enabled = true;
            
            isTeleporting = false;
        }
        
        #endregion

        #region RPC

        [Rpc(SendTo.Owner)]
        private void OnReleasePrisonClientRpc()
        {
            OnReleasePrison();
        }

        [Rpc(SendTo.Owner)]
        private void TeleportToSpawnPointClientRpc(Vector3 spawnPosition, bool isLobby)
        {
            StartCoroutine(TeleportToSpawnAndApplyRestrictions(spawnPosition, isLobby));
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
           
        }

        private void OnGameEnd()
        {
            if (IsServer)
            {
                isImprisoned.Value = false;
                StartCoroutine(OnGameEndIe());
            }
        }

        private IEnumerator OnGameEndIe()
        {
            yield return new WaitForSeconds(2);
            
            Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(true);
            TeleportToSpawnPointClientRpc(spawnPos, true);
        }

        private void OnGameStart()
        {
            if (IsServer)
            {
                Vector3 spawnPos = SpawnManager.Instance.GetSpawnPosition(false);
                TeleportToSpawnPointClientRpc(spawnPos, false);
            }
        }

        #endregion
        
        #region Seeker Restriction
        
        private IEnumerator TeleportToSpawnAndApplyRestrictions(Vector3 spawnPosition, bool isLobby)
        {
            yield return StartCoroutine(TeleportToPositionCoroutine(spawnPosition, false));
            
        }
       

        #endregion

        private void Xray()
        {
            if (!isSeeker.Value && IsServer)
            {
                ActivateXrayClientRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ActivateXrayClientRpc()
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