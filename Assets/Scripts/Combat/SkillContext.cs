using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Per-execution mutable data container for one skill action.
    /// Rebuilt for each skill use. Consumed by combat calculation and effect stages.
    /// </summary>
    public class SkillContext
    {
        // --- Core Data ---
        public CombatCharacter user;
        public SkillData skill;
        public List<CombatCharacter> targets;
        public CombatCharacter primaryTarget;

        // --- Combat Calculation ---
        public int baseAttackRoll;
        public float skillScaling;
        public int calculatedValue;
        public float damageMultiplier = 1f;

        // --- Critical System ---
        public bool isCritical;
        public float critMultiplier = 1.5f;

        // --- Hit Resolution ---
        public float finalAccuracy;
        public bool didHit;

        // --- Special Interaction Flags ---
        public bool ignoresDefense;
        public bool ignoresDodge;
        public bool guaranteedHit;
        public bool bypassGuard;

        // --- Multi-Hit Tracking ---
        public int totalHits = 1;
        public int currentHitIndex = 0;

        // --- Status System ---
        public List<StatusEffectInstance> pendingStatuses = new List<StatusEffectInstance>();

        // --- System References ---
        public BattleSystem battleSystem;
        public System.Random rng;

        // --- Flexible Extension ---
        public Dictionary<string, object> extra = new Dictionary<string, object>();

        public SkillContext(CombatCharacter user, SkillData skill, List<CombatCharacter> targets,
                            BattleSystem battleSystem, System.Random rng)
        {
            this.user = user;
            this.skill = skill;
            this.targets = targets ?? new List<CombatCharacter>();
            this.primaryTarget = this.targets.Count > 0 ? this.targets[0] : null;
            this.battleSystem = battleSystem;
            this.rng = rng;

            // Copy special flags from skill
            this.ignoresDefense = skill.ignoresDefense;
            this.ignoresDodge = skill.ignoresDodge;
            this.guaranteedHit = skill.guaranteedHit;
            this.bypassGuard = skill.bypassGuard;
            this.totalHits = skill.hitCount;
            this.skillScaling = skill.modifier.IsDamage ? skill.modifier.damagePercent
                              : skill.modifier.IsHeal ? skill.modifier.healPercent
                              : 0f;
        }
    }
}
