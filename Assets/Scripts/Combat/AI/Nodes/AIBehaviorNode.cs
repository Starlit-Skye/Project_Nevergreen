using System;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Abstract base class for AI Behaviors to support polymorphic serialization.
    /// </summary>
    [Serializable]
    public abstract class AIBehaviorNode : IAIBehavior
    {
        public abstract bool TryGetDecision(AIBrain brain, BattleSystem battle, out AIDecision decision);
    }
}