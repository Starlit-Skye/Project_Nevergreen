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

        // Status effect tooltip events
        public static event Action<StatusIconTooltipTrigger> OnShowStatusTooltip;
        public static event Action<StatusIconTooltipTrigger> OnHideStatusTooltip;

        public static void ShowStatusTooltip(StatusIconTooltipTrigger trigger)
        {
            OnShowStatusTooltip?.Invoke(trigger);
        }

        public static void HideStatusTooltip(StatusIconTooltipTrigger trigger)
        {
            OnHideStatusTooltip?.Invoke(trigger);
        }

        // Trait tooltip events
        public static event Action<Nevergreen.Data.TraitData> OnShowTraitTooltip;
        public static event Action OnHideTraitTooltip;

        public static void ShowTraitTooltip(Nevergreen.Data.TraitData trait)
        {
            OnShowTraitTooltip?.Invoke(trait);
        }

        public static void HideTraitTooltip()
        {
            OnHideTraitTooltip?.Invoke();
        }

        // Trinket tooltip events
        public static event Action<Nevergreen.Data.TrinketData> OnShowTrinketTooltip;
        public static event Action OnHideTrinketTooltip;

        public static void ShowTrinketTooltip(Nevergreen.Data.TrinketData trinket)
        {
            OnShowTrinketTooltip?.Invoke(trinket);
        }

        public static void HideTrinketTooltip()
        {
            OnHideTrinketTooltip?.Invoke();
        }
    }
}
