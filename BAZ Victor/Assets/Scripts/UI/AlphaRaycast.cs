using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class AlphaRaycast : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        
        [Header("Sprite Settings")]
        [SerializeField] private Sprite normalSprite;   
        [SerializeField] private Sprite hoverSprite;    
        [SerializeField] private Image buttonImage;
        [SerializeField] private float alphaThresold = 0.1f;

        private void Awake()
        {
            
            if (buttonImage != null && normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }

            buttonImage.alphaHitTestMinimumThreshold = alphaThresold;
        }
        

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonImage != null && hoverSprite != null)
            {
                buttonImage.sprite = hoverSprite;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (buttonImage != null && normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (buttonImage != null && hoverSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }
            
        }
    }
}
