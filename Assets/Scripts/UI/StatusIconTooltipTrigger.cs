using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Combat;

namespace Nevergreen.UI
{
    /// <summary>
    /// Component attached to the StatusIconPrefab.
    /// Detects pointer hover events and raises global status tooltip events.
    /// </summary>
    public class StatusIconTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private StatusEffectInstance _statusEffect;

        public StatusEffectInstance StatusEffect => _statusEffect;

        public void Initialize(StatusEffectInstance statusEffect)
        {
            _statusEffect = statusEffect;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[StatusIconTooltipTrigger] OnPointerEnter triggered for {_statusEffect?.type}");
            if (_statusEffect != null)
            {
                TooltipEvents.ShowStatusTooltip(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_statusEffect != null)
            {
                TooltipEvents.HideStatusTooltip(this);
            }
        }

        private void OnDisable()
        {
            if (_statusEffect != null)
            {
                TooltipEvents.HideStatusTooltip(this);
            }
        }
    }
}
