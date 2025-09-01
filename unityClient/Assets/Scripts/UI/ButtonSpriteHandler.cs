using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class ButtonSpriteHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject hoverObject;
        
        private void Start()
        {
            if (hoverObject != null)
            {
                hoverObject.SetActive(false);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverObject != null)
            {
                hoverObject.SetActive(true);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverObject != null)
            {
                hoverObject.SetActive(false);
            }
        }
        
        private void OnDisable()
        {
            if (hoverObject != null)
            {
                hoverObject.SetActive(false);
            }
        }
    }
}