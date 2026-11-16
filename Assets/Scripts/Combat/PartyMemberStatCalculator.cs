using System.Collections.Generic;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Computes effective (trait- and trinket-modified) stats for a <see cref="PartyMemberInfo"/>
    /// outside of combat, reusing the exact combat stat modification formula from <see cref="CombatCharacter"/>.
    /// </summary>
    public static class PartyMemberStatCalculator
    {
        /// <summary>
        /// Returns the effective Max HP for the given party member after applying all active
        /// Perfection, Imperfection, and Trinket passive stat modifiers. Clamped at minimum 1.
        /// </summary>
        public static int GetEffectiveMaxHP(PartyMemberInfo member)
        {
            if (member == null || member.character == null)
            {
                return 1;
            }

            var baseStatBlock = member.character.GetStatsForLevel(member.currentLevel);
            int baseMaxHP = baseStatBlock != null ? baseStatBlock.maxHP : 1;

            var traitMod = new TraitStatModifier();

            if (member.perfections != null)
            {
                for (int i = 0; i < member.perfections.Count; i++)
                {
                    var traitData = member.perfections[i];
                    if (traitData != null)
                    {
                        var instance = new TraitInstance(traitData, null, null);
                        instance.ModifyStats(traitMod);
                    }
                }
            }

            if (member.imperfections != null)
            {
                for (int i = 0; i < member.imperfections.Count; i++)
                {
                    var traitData = member.imperfections[i];
                    if (traitData != null)
                    {
                        var instance = new TraitInstance(traitData, null, null);
                        instance.ModifyStats(traitMod);
                    }
                }
            }

            if (member.equippedTrinkets != null)
            {
                for (int i = 0; i < member.equippedTrinkets.Count; i++)
                {
                    var trinketData = member.equippedTrinkets[i];
                    if (trinketData != null)
                    {
                        var instance = new TrinketInstance(trinketData, null, null);
                        instance.ModifyStats(traitMod);
                    }
                }
            }

            var netPercent = new Dictionary<StatTarget, float>();
            foreach (var kvp in traitMod.percentBonuses)
            {
                netPercent[kvp.Key] = kvp.Value / 100f;
            }

            return CombatCharacter.GetModifiedStatValue(baseMaxHP, StatTarget.MaxHP, netPercent, traitMod.flatBonuses);
        }
    }
}

