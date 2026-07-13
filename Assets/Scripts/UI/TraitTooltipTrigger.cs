using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Attached to an individual Trait UI item prefab.
    /// Detects mouse hover and fires global trait tooltip events.
    /// </summary>
    public class TraitTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TraitData _trait;

        public void SetTrait(TraitData trait)
        {
            _trait = trait;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_trait != null)
            {
                TooltipEvents.ShowTraitTooltip(_trait);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipEvents.HideTraitTooltip();
        }

        private void OnDisable()
        {
            TooltipEvents.HideTraitTooltip();
        }
    }
}
