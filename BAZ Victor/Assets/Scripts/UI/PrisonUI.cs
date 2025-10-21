using System;
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
            if (prisonerCountText != null)
            {
                int count = prisonZone.GetPrisonerCount();
                prisonerCountText.text = $"Prisonniers: {count}";
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
    }
}
