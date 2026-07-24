using Nevergreen.Combat;
using System;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Single target attacks always crit, and AOE attacks always hit.
    /// </summary>
    [Serializable]
    public class SingleTargetCritAoeHitTrinketStrategy : TrinketEffectStrategy
    {
        public override void OnActivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            // Handle AOE targeting all enemies
            Action<SkillContext> aoeHitHandler = (ctx) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.maxTargets > 1 && ctx.skill.targetScope == TargetScope.Enemies)
                {
                    var allEnemies = ctx.user.IsPlayerTeam ? instance.battleSystem.EnemyTeam : instance.battleSystem.PlayerTeam;
                    ctx.targets.Clear();
                    foreach (var enemy in allEnemies)
                    {
                        ctx.targets.Add(enemy);
                    }
                }
            };
            string key1 = $"OnBeforeDamageCalculation_AoeHit_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key1] = aoeHitHandler;
            instance.battleSystem.OnBeforeDamageCalculation += aoeHitHandler;

            // Handle Single Target Guaranteed Crit
            Action<SkillContext, CombatCharacter> stCritHandler = (ctx, target) =>
            {
                if (ctx.user == instance.owner && ctx.skill != null && ctx.skill.maxTargets == 1 && ctx.skill.modifier.IsDamage)
                {
                    ctx.isCritical = true;
                }
            };
            string key2 = $"OnBeforeDamageCalculationPerTarget_StCrit_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            instance.extra[key2] = stCritHandler;
            instance.battleSystem.OnBeforeDamageCalculationPerTarget += stCritHandler;
        }

        public override void OnDeactivate(TrinketInstance instance)
        {
            if (instance.battleSystem == null) return;

            string key1 = $"OnBeforeDamageCalculation_AoeHit_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key1, out object handlerObj1) && handlerObj1 is Action<SkillContext> handler1)
            {
                instance.battleSystem.OnBeforeDamageCalculation -= handler1;
                instance.extra.Remove(key1);
            }

            string key2 = $"OnBeforeDamageCalculationPerTarget_StCrit_{instance.owner.GetInstanceID()}_{GetHashCode()}";
            if (instance.extra.TryGetValue(key2, out object handlerObj2) && handlerObj2 is Action<SkillContext, CombatCharacter> handler2)
            {
                instance.battleSystem.OnBeforeDamageCalculationPerTarget -= handler2;
                instance.extra.Remove(key2);
            }
        }

        public override string GetTooltipDescription()
        {
            return "Single-target attacks always crit. Area attacks always hit.";
        }
    }
}
