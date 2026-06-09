using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Generates random Marionettes (PartyMemberInfo) with randomized skills and traits.
    /// </summary>
    public static class MarionetteGenerator
    {
        private static System.Random _rng = new System.Random();

        /// <summary>
        /// Generates a randomized PartyMemberInfo based on the provided databases and configuration.
        /// </summary>
        public static PartyMemberInfo GenerateRandomMarionette(MarionetteDatabase db, TraitDatabase traitDb, CombatConfig config)
        {
            if (db == null || db.marionettes == null || db.marionettes.Count == 0)
            {
                Debug.LogError("[MarionetteGenerator] MarionetteDatabase is null or empty!");
                return null;
            }

            // 1. Select a random class template
            var template = db.marionettes[_rng.Next(db.marionettes.Count)];

            var info = new PartyMemberInfo
            {
                character = template
            };

            // 2. Select exactly 4 unique random skills (if available)
            var pool = template.totalSkillPool != null && template.totalSkillPool.Count > 0
                ? template.totalSkillPool
                : template.availableSkills;

            if (pool != null && pool.Count > 0)
            {
                // Create a temporary copy to pick from without replacement
                var availablePool = new List<SkillData>(pool);
                int skillsToPick = Mathf.Min(4, availablePool.Count);

                for (int i = 0; i < skillsToPick; i++)
                {
                    int index = _rng.Next(availablePool.Count);
                    info.equippedSkills.Add(availablePool[index]);
                    availablePool.RemoveAt(index);
                }
            }

            // 3. Assign 1 random Perfection and 1 random Imperfection
            if (traitDb != null)
            {
                if (traitDb.perfections != null && traitDb.perfections.Count > 0)
                {
                    var perf = traitDb.perfections[_rng.Next(traitDb.perfections.Count)];
                    info.TryAddTrait(perf, config);
                }

                if (traitDb.imperfections != null && traitDb.imperfections.Count > 0)
                {
                    var imperf = traitDb.imperfections[_rng.Next(traitDb.imperfections.Count)];
                    info.TryAddTrait(imperf, config);
                }
            }
            else
            {
                Debug.LogWarning("[MarionetteGenerator] TraitDatabase is null. Traits will not be assigned.");
            }

            return info;
        }
    }
}
