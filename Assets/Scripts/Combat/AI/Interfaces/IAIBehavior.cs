using Nevergreen.Combat;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// Base interface for all AI decision blocks.
    /// </summary>
    public interface IAIBehavior
    {
        /// <summary>
        /// Evaluates the behavior and attempts to produce a combat decision.
        /// </summary>
        /// <param name="brain">The AI Brain instance executing this behavior.</param>
        /// <param name="battle">The current battle state.</param>
        /// <param name="decision">The resulting decision if successful.</param>
        /// <returns>True if the behavior successfully resolved a decision, false otherwise.</returns>
        bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision);
    }
}
