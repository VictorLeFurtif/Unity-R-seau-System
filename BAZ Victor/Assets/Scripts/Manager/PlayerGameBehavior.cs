using System;
using System.Collections;
using Enum;
using Fps_Handle.Scripts.Controller;
using Unity.Netcode;
using UnityEngine;

namespace Manager
{
    public class PlayerGameBehavior : NetworkBehaviour
    {
        #region Fields

        [SerializeField] private NetworkVariable<bool> isSeeker = new NetworkVariable<bool>(false);
        [SerializeField] private NetworkVariable<bool> isImprisoned = new NetworkVariable<bool>(false);
        
        private GameObject prisonGameObject;
        
        private Vector3 prisonMin;
        private Vector3 prisonMax;

        private PlayerController pc;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
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
                Debug.LogError(" PRISON PAS LAAAAAA");
            }
            
            BoxCollider col = prisonGameObject.GetComponent<BoxCollider>();
            
            if (col == null)
            {
                Debug.LogError(" COLLIDER PAS LAAAAAA");
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

            if (IsOwner)
            {
                //pc.SetterMove(false);
            }
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
            Vector3 prisonPos = prisonGameObject.transform.position + new Vector3(0,2,0); //offsett ?

            Rigidbody playerRb = pc.GetPlayerRigidbody();
            
            playerRb.isKinematic = true;
            yield return new WaitForFixedUpdate();
            transform.position = prisonPos;
            yield return new WaitForFixedUpdate();
            playerRb.isKinematic = false;
            
            pc.ResetVelocity();
            transform.position = prisonPos; //to make sure
            
            PrisonZone prisonZone = prisonGameObject.GetComponent<PrisonZone>();
            prisonZone.AddPrisoner(this);
        }
        
        #endregion

        #region RPC

        [Rpc(SendTo.Everyone)]
        private void SetImprisonedRpc(bool value)
        {
            if (IsServer) //on met a jour que le serv/host vu que c'est une networkVariable
            {
                isImprisoned.Value = value;
            }
    
            //on met a jour que la pos pour tout le monde en suite
            if (value)
                OnCapturePrison();
            else
                OnReleasePrison();
        }

        #endregion

        #region Getter Setter

        public bool IsSeeker() => isSeeker.Value;

        public bool IsImprisoned() => isImprisoned.Value;

        #endregion
    }
}
