using System;
using EventBus;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PrisonUI : MonoBehaviour
    {
        #region Fields
        
        [Header("References")]
        [SerializeField] private PrisonZone prisonZone;
        
        [Header("UI Elements")]
        [SerializeField] private Slider releaseProgressBar;
        [SerializeField] private TMP_Text prisonerCountText;
        [SerializeField] private GameObject releasePanel;
        [SerializeField] private GameObject countPanel;

        #endregion

        #region Unity Methods

        private void Start()
        {
            InitComponent();
        }

        private void Update()
        {
            UpdateUI();
        }

        #endregion

        #region Init

        private void InitComponent()
        {
            if (prisonZone == null) prisonZone = GetComponent<PrisonZone>();
            
            if (releasePanel != null) releasePanel.SetActive(false);

            releaseProgressBar.maxValue = prisonZone.GetReleaseProgress();
            releaseProgressBar.value = prisonZone.GetReleaseProgress();
        }

        #endregion
        
        #region UI Update
        
        private void UpdateUI()
        {
            if (!GameManager.Instance.InGame())
            {
                return;
            }
            
            if (prisonerCountText != null)
            {
                int count = prisonZone.GetPrisonerCount();
                prisonerCountText.text = $"Prisonniers: {count}/{GameManager.Instance.GetPlayerCount() - 1}";
            }
            
            bool isReleasing = prisonZone.IsReleasing();
            
            if (releasePanel != null)
            {
                releasePanel.SetActive(isReleasing);
            }
            
            if (releaseProgressBar != null && isReleasing)
            {
                float progress = prisonZone.GetReleaseProgress();
                releaseProgressBar.value = progress;
            }
        }
        
        #endregion
        
        #region Event Listeners

        private void OnEnable()
        {
            EventManager.OnMenu += OnMenuState;
            EventManager.OnLobbyEntered += OnLobbyState;
            EventManager.OnGameStarted += OnGameState;
            EventManager.OnGameEnded += OnGameEndState;
        }

        private void OnDisable()
        {
            EventManager.OnMenu -= OnMenuState;
            EventManager.OnLobbyEntered -= OnLobbyState;
            EventManager.OnGameStarted -= OnGameState;
            EventManager.OnGameEnded -= OnGameEndState;
        }

        #endregion
        
        #region State Handlers
        
        private void OnMenuState()
        {
            releasePanel.SetActive(false);
            countPanel.SetActive(false);
        }
        
        private void OnLobbyState()
        {
            releasePanel.SetActive(false);
            countPanel.SetActive(false);
        }
        
        private void OnGameState()
        {
            releasePanel.SetActive(true);
            countPanel.SetActive(true);
        }
        
        private void OnGameEndState()
        {
            releasePanel.SetActive(false);
            countPanel.SetActive(true);
        }
        
        #endregion
    }
}
