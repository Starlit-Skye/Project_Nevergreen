using System;
using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Combat.AI.Nodes
{
    /// <summary>
    /// Targets a specific character based on a matching CharacterData asset ID.
    /// Returns false if the specific character is not in the valid target pool or the data is not provided.
    /// </summary>
    [Serializable]
    public class SpecificCharacterTargeting : AITargetingNode
    {
        [Tooltip("The CharacterData asset of the character to target.")]
        public CharacterData targetCharacterData;

        public override bool TryResolveTargets(AIBrain brain, BattleSystem battle, SkillData skill, out List<CombatCharacter> targets)
        {
            targets = null;

            if (targetCharacterData == null || string.IsNullOrEmpty(targetCharacterData.characterId))
            {
                return false;
            }

            List<CombatCharacter> pool = battle.GetValidTargets(brain.Self, skill);
            
            // Search the pool for the specific character
            CombatCharacter matchedCharacter = null;
            foreach (var character in pool)
            {
                if (character.CharacterId == targetCharacterData.characterId)
                {
                    matchedCharacter = character;
                    break;
                }
            }

            if (matchedCharacter == null)
            {
                return false; // Character not found or not in valid pool
            }

            // Target found, resolve AOE starting from this primary target
            targets = battle.GetAOETargets(skill, matchedCharacter);
            return true;
        }
    }
}
