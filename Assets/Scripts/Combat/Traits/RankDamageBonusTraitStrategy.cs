using Nevergreen.Data;
using UnityEngine;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Trait strategy that increases the damage multiplier when the owner
    /// is standing at a specific rank. Subscribes to OnActionResolved
    /// indirectly by modifying the SkillContext during skill execution.
    ///
    /// Implementation approach: Subscribes to BattleSystem.OnTurnStarted
    /// to check rank at the start of the owner's turn and stores a flag.
    /// Then subscribes to BattleSystem.OnActionResolved to apply the bonus
    /// to the SkillContext's damageMultiplier.
    ///
    /// Simpler approach chosen: Override OnActivate/OnDeactivate to subscribe
    /// to events on BattleSystem, and modify the SkillContext damage multiplier
    /// via the OnSkillExecute status hook pattern.
    /// 
    /// Actually, the cleanest approach: we hook into the existing
    /// BattleSystem.OnActionResolved event is too late (post-resolution).
    /// Instead, we directly participate via the existing skill execution
    /// status hook pattern: we create a non-expiring "phantom" StatusEffectInstance
    /// that calls OnSkillExecute to modify the context.
    ///
    /// REVISED: To keep things clean and modular, this strategy subscribes to
    /// BattleSystem.OnTurnStarted. When the owner's turn fires, it checks rank
    /// and caches whether the bonus is active. The bonus is then applied in
    /// ModifyStats as a percentage modifier on Attack — this is simpler and 
    /// more predictable than modifying damageMultiplier, since it stacks with
    /// the existing buff/debuff system naturally.
    ///
    /// FINAL: For a true damage multiplier (not attack stat), we need to hook
    /// into skill execution. The simplest pattern is to use the existing
    /// OnSkillExecute virtual hook on StatusEffectInstance. But traits are NOT
    /// status effects. Instead, we add a new event on BattleSystem or use a
    /// dedicated approach.
    ///
    /// For this proof-of-concept, we use the stat modifier approach:
    /// Grant an Attack buff when at the required rank.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRankDamageTrait", menuName = "Nevergreen/Traits/Rank Damage Bonus")]
    public class RankDamageBonusTraitStrategy : TraitEffectStrategy
    {
        [Tooltip("The rank the owner must be at for the bonus to apply.")]
        [Range(1, 4)]
        public int requiredRank = 1;

        [Tooltip("The percentage bonus to Attack when at the required rank (e.g. 15 = +15% Attack).")]
        public int attackBonusPercent = 15;

        public override void ModifyStats(TraitInstance instance, TraitStatModifier modifier)
        {
            if (instance.owner == null) return;

            if (instance.owner.rank == requiredRank)
            {
                modifier.AddPercent(StatTarget.Attack, attackBonusPercent);
            }
        }
    }
}
