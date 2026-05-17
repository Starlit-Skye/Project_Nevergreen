using System;
using System.Collections.Generic;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Abstract base class for AI Targeting to support polymorphic serialization.
    /// </summary>
    [Serializable]
    public abstract class AITargetingNode : IAITargeting
    {
        public abstract bool TryResolveTargets(AIBrain brain, BattleSystem battle, SkillData skill, out List<CombatCharacter> targets);
    }
}