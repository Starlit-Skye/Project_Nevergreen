namespace Nevergreen.Data
{
    /// <summary>
    /// Determines when a room effect strategy is executed.
    /// Configured per-room in the RoomData ScriptableObject.
    /// </summary>
    public enum RoomActivationType
    {
        /// <summary>Bypasses standard combat bootstrap and runs immediately upon scene load.</summary>
        OnRoomLoaded,

        /// <summary>Standard combat proceeds; strategy activates at start of combat to apply lingering effects.</summary>
        ContinuousCombat,

        /// <summary>Strategy activates when combat ends in victory.</summary>
        OnCombatVictory
    }
}
