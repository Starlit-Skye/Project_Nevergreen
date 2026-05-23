using UnityEngine;
using UnityEngine.EventSystems;
using Nevergreen.Data;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Detects mouse hover events on UI elements and forwards them to the SkillTooltipManager.
    /// </summary>
    public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SkillData _skillData;
        private SkillTooltipManager _tooltipManager;

        /// <summary>
        /// Sets the skill and tooltip manager references.
        /// </summary>
        public void SetSkill(SkillData skillData, SkillTooltipManager tooltipManager)
        {
            _skillData = skillData;
            _tooltipManager = tooltipManager;
        }

        /// <summary>
        /// Clears the assigned skill reference.
        /// </summary>
        public void ClearSkill()
        {
            _skillData = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skillData != null && _tooltipManager != null)
            {
                _tooltipManager.ShowTooltip(_skillData);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipManager != null)
            {
                _tooltipManager.HideTooltip();
            }
        }

        private void OnDisable()
        {
            // If the element is disabled, make sure we hide the tooltip so it doesn't stay visible.
            if (_tooltipManager != null)
            {
                _tooltipManager.HideTooltip();
            }
        }
    }
}
