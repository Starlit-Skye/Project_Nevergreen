using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Attached to UI elements (like buttons) to broadcast tooltip events when hovered.
    /// Uses IPointerEnterHandler and IPointerExitHandler for hover detection.
    /// </summary>
    public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SkillData _skillData;
        private bool _isHovered = false;

        public void SetSkill(SkillData skillData)
        {
            _skillData = skillData;
            
            // If we're currently hovering and the skill changes, update the tooltip immediately
            if (_isHovered)
            {
                if (_skillData != null && !string.IsNullOrEmpty(_skillData.description))
                {
                    TooltipEvents.ShowTooltip(_skillData.description);
                }
                else
                {
                    TooltipEvents.HideTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (_skillData != null && !string.IsNullOrEmpty(_skillData.description))
            {
                TooltipEvents.ShowTooltip(_skillData.description);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            TooltipEvents.HideTooltip();
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                _isHovered = false;
                TooltipEvents.HideTooltip();
            }
        }
    }
}
