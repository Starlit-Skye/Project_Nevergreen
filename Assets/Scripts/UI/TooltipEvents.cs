using System;

namespace Nevergreen.UI
{
    /// <summary>
    /// Global event broker for the reusable tooltip system.
    /// Decouples the UI hover triggers from the tooltip display logic.
    /// </summary>
    public static class TooltipEvents
    {
        public static event Action<string> OnShowTooltip;
        public static event Action OnHideTooltip;

        public static void ShowTooltip(string description)
        {
            OnShowTooltip?.Invoke(description);
        }

        public static void HideTooltip()
        {
            OnHideTooltip?.Invoke();
        }
    }
}
