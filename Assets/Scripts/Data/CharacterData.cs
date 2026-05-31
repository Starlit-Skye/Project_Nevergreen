using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Defines a character template (marionette or enemy).
    /// Stats are resolved at runtime via statPerLevel[currentLevel - 1].
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Nevergreen/Data/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Tooltip("Unique identifier for this character.")]
        public string characterId;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("The corresponding visual prefab for this character.")]
        public Nevergreen.Combat.CombatCharacter characterPrefab;

        [Tooltip("Whether this is a player unit or enemy unit.")]
        public CharacterTeamType teamType = CharacterTeamType.Enemy;

        [Tooltip("Number of actions this character gets per round. Most characters = 1, some bosses = 2.")]
        [Min(1)]
        public int actionsPerRound = 1;

        [Tooltip("Stat block for each level. Index 0 = level 1, index 1 = level 2, etc.")]
        public List<StatBlockData> statPerLevel = new List<StatBlockData>();

        [Tooltip("Skills available to this character. Up to 4 can be used in battle.")]
        public List<SkillData> availableSkills = new List<SkillData>();

        [Tooltip("The total pool of skills this character can select from in the skill selection menu.")]
        public List<SkillData> totalSkillPool = new List<SkillData>();

        [Tooltip("How many contiguous ranks this character occupies (1 = normal, 2-4 = large/boss).")]
        [Range(1, 4)]
        public int size = 1;

        [Tooltip("If true, this character leaves a Pile (corpse) on non-critical death. If false, they are destroyed immediately.")]
        public bool leavesPileOnDeath = true;

        [Tooltip("The default AI Profile to use if this character is an enemy. Ignored for Player characters.")]
        public Nevergreen.Combat.AI.EnemyAIProfile defaultAIProfile;

        /// <summary>
        /// Resolves the stat block for the given level using current_level - 1 indexing.
        /// Clamps to valid range.
        /// </summary>
        public StatBlockData GetStatsForLevel(int level)
        {
            if (statPerLevel == null || statPerLevel.Count == 0)
            {
                Debug.LogError($"[CharacterData] '{displayName}' has no stat entries.");
                return null;
            }

            int index = Mathf.Clamp(level - 1, 0, statPerLevel.Count - 1);
            return statPerLevel[index];
        }

        [Header("Audio")]
        [Tooltip("Sound effect played when this character is defeated.")]
        public AudioClip deathSFX;

        [Tooltip("If set, this music will play when a battle starts with this character in the enemy team.")]
        public AudioClip bossMusicOverride;
    }

    public enum CharacterTeamType
    {
        Player,
        Enemy
    }
}
