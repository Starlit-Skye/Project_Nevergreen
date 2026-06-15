using System.Collections.Generic;
using UnityEngine;
using Nevergreen;

namespace Nevergreen.Data
{
    /// <summary>
    /// Generates random Marionettes (PartyMemberInfo) with randomized skills and traits.
    /// Reads database data from the centralized GameDatabase.Instance.
    /// </summary>
    public static class MarionetteGenerator
    {
        private static System.Random _rng = new System.Random();

        /// <summary>
        /// Generates a list of randomized PartyMemberInfo based on the centralized GameDatabase and configuration.
        /// Enforces unique classes and ensures a healer is present if the current party lacks one.
        /// </summary>
        public static List<PartyMemberInfo> GenerateRandomMarionette(int count, CombatConfig config)
        {
            var gameDb = GameDatabase.Instance;
            if (gameDb == null)
            {
                Debug.LogError("[MarionetteGenerator] GameDatabase.Instance is null!");
                return null;
            }

            var db = gameDb.MarionetteDatabase;
            var traitDb = gameDb.TraitDatabase;

            if (db == null || db.marionettes == null || db.marionettes.Count == 0)
            {
                Debug.LogError("[MarionetteGenerator] MarionetteDatabase is null or empty!");
                return null;
            }

            var generatedList = new List<PartyMemberInfo>();
            if (count <= 0) return generatedList;

            // 1. Check for healer option in Current Party
            bool hasHealer = HasHealerOption(RunSessionManager.CurrentParty);

            // 2. Separate templates into healer pool and other pool
            var healerPool = new List<CharacterData>();
            var otherPool = new List<CharacterData>();

            foreach (var template in db.marionettes)
            {
                if (IsHealerClass(template))
                {
                    healerPool.Add(template);
                }
                else
                {
                    otherPool.Add(template);
                }
            }

            var selectedTemplates = new List<CharacterData>();

            // 3. Force a healer if none exists
            if (!hasHealer && healerPool.Count > 0)
            {
                int index = _rng.Next(healerPool.Count);
                selectedTemplates.Add(healerPool[index]);
                healerPool.RemoveAt(index);
                count--;
            }

            // 4. Combine remaining and pick the rest randomly
            var unifiedPool = new List<CharacterData>();
            unifiedPool.AddRange(healerPool);
            unifiedPool.AddRange(otherPool);

            for (int i = 0; i < count && unifiedPool.Count > 0; i++)
            {
                int index = _rng.Next(unifiedPool.Count);
                selectedTemplates.Add(unifiedPool[index]);
                unifiedPool.RemoveAt(index);
            }

            // 5. Generate units from selected templates
            foreach (var template in selectedTemplates)
            {
                generatedList.Add(GenerateMarionetteFromTemplate(template, traitDb, config));
            }

            return generatedList;
        }

        private static bool HasHealerOption(List<PartyMemberInfo> party)
        {
            if (party == null) return false;

            foreach (var member in party)
            {
                if (member == null || member.character == null) continue;

                var data = member.character;
                
                // Check if the class is a healer class
                if (IsHealerClass(data))
                {
                    return true;
                }

                // Check if Cecilia has Hasty Repair
                if (data.characterId == "ceci" || data.displayName == "Cecilia")
                {
                    if (member.equippedSkills != null)
                    {
                        foreach (var skill in member.equippedSkills)
                        {
                            if (skill != null && (skill.skillId == "hasty_repair" || skill.displayName == "Hasty Repair"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsHealerClass(CharacterData template)
        {
            if (template == null) return false;
            
            return template.characterId == "maid_marionette" || template.displayName == "Maid" ||
                   template.characterId == "commander_marionette" || template.displayName == "Commander" ||
                   template.characterId == "alchemist_marionette" || template.displayName == "Alchemist";
        }

        /// <summary>
        /// Populates skills and traits for a chosen template.
        /// traitDb is passed internally from the centralized database.
        /// </summary>
        public static PartyMemberInfo GenerateMarionetteFromTemplate(CharacterData template, TraitDatabase traitDb, CombatConfig config)
        {
            var info = new PartyMemberInfo
            {
                character = template
            };

            // Select exactly 4 unique random skills (if available)
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

            // Assign 1 random Perfection and 1 random Imperfection
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
