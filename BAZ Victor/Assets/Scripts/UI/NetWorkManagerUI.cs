using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class NetWorkManagerUI : MonoBehaviour
    {
        #region Fields
        
        [Header("Button Ref")]
        [SerializeField] private Button buttonHost;
        [SerializeField] private Button buttonServer;
        [SerializeField] private Button buttonClient;
        
        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitButtonBehavior();
        }

        #endregion

        #region Init

        private void InitButtonBehavior()
        {
            buttonServer?.onClick.AddListener(()=> NetworkManager.Singleton.StartServer());
            buttonHost?.onClick.AddListener(()=> NetworkManager.Singleton.StartHost());
            buttonClient?.onClick.AddListener(()=> NetworkManager.Singleton.StartClient());
            buttonServer?.onClick.AddListener(OnTEST);
            buttonHost?.onClick.AddListener(OnTEST);
            buttonClient?.onClick.AddListener(OnTEST);
        }

        private void OnTEST()
        {
            Debug.Log("TEST");
        }

        #endregion
        
    }
}
