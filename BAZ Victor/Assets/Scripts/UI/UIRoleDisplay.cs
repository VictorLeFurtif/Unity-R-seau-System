using EventBus;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIRoleDisplay : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image roleImage;

        [Header("Sprites")]
        [SerializeField] private Sprite seekerSprite;
        [SerializeField] private Sprite hiderSprite;

        private void UpdateRoleUI()
        {
            roleImage.sprite = NetworkManager.Singleton.IsHost ? seekerSprite : hiderSprite;
        }

        #region Observer

        private void OnEnable()
        {
            roleImage.gameObject.SetActive(false);

            EventManager.OnGameStarted += OnGameState;
        }

        private void OnGameState()
        {
            UpdateRoleUI();
            roleImage.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            EventManager.OnGameStarted -= OnGameState;
        }

        #endregion
    }
}