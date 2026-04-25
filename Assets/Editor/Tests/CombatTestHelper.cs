using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    /// <summary>
    /// Shared test utilities for creating combat test fixtures.
    /// Avoids MonoBehaviour dependency by using ScriptableObject.CreateInstance
    /// and direct field assignment.
    /// </summary>
    public static class CombatTestHelper
    {
        /// <summary>
        /// Create a minimal CombatConfig with GDD defaults.
        /// </summary>
        public static CombatConfig CreateDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.attackRollMin = 0.8f;
            config.attackRollMax = 1.2f;
            config.accuracyCap = 95;
            config.defenseCap = 95;
            config.dodgeCap = 95;
            config.critDamageMultiplier = 1.5f;
            config.stunRecoveryResistBonus = 300;
            config.maxPartySize = 4;
            config.rankCount = 4;
            config.globalMaxLevel = 10;
            return config;
        }

        /// <summary>
        /// Create a StatBlockData with explicit stat values.
        /// </summary>
        public static StatBlockData CreateStatBlock(
            int attack = 100, int defense = 0, int accuracy = 95,
            int dodge = 5, int critChance = 5, int speed = 5,
            int maxHP = 100, int bleedResist = 0, int blightResist = 0,
            int stunResist = 0, int debuffResist = 0, int moveResist = 0)
        {
            var block = ScriptableObject.CreateInstance<StatBlockData>();
            block.attack = attack;
            block.defense = defense;
            block.accuracy = accuracy;
            block.dodge = dodge;
            block.critChance = critChance;
            block.speed = speed;
            block.maxHP = maxHP;
            block.bleedResist = bleedResist;
            block.blightResist = blightResist;
            block.stunResist = stunResist;
            block.debuffResist = debuffResist;
            block.moveResist = moveResist;
            return block;
        }

        /// <summary>
        /// Create a CharacterData with one level of stats.
        /// </summary>
        public static CharacterData CreateCharacterData(
            string id, string displayName, StatBlockData stats,
            CharacterTeamType teamType = CharacterTeamType.Player)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = id;
            data.displayName = displayName;
            data.teamType = teamType;
            data.actionsPerRound = 1;
            data.statPerLevel = new List<StatBlockData> { stats };
            data.availableSkills = new List<SkillData>();
            return data;
        }

        /// <summary>
        /// Create a CombatCharacter on a temporary GameObject, initialized for combat.
        /// Caller is responsible for cleanup via GameObject.DestroyImmediate.
        /// </summary>
        public static CombatCharacter CreateCombatCharacter(
            string id, Team team, int rank,
            int attack = 100, int defense = 0, int accuracy = 95,
            int dodge = 5, int critChance = 5, int speed = 5,
            int maxHP = 100, int stunResist = 0, int debuffResist = 0,
            CombatConfig config = null)
        {
            var go = new GameObject($"TestCharacter_{id}");
            var cc = go.AddComponent<CombatCharacter>();

            var stats = CreateStatBlock(attack, defense, accuracy, dodge,
                                        critChance, speed, maxHP,
                                        stunResist: stunResist, debuffResist: debuffResist);
            var charData = CreateCharacterData(id, id, stats,
                team == Team.Player ? CharacterTeamType.Player : CharacterTeamType.Enemy);

            cc.characterData = charData;
            cc.currentLevel = 1;
            cc.combatConfig = config ?? CreateDefaultConfig();
            cc.InitializeForCombat(team, rank);

            return cc;
        }

        /// <summary>
        /// Create a minimal SkillData for damage testing.
        /// </summary>
        public static SkillData CreateDamageSkill(
            float damagePercent = 1.0f, float accuracyMod = 0f,
            float critMod = 0f, bool guaranteedHit = false,
            bool ignoresDefense = false, bool ignoresDodge = false)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "test_damage";
            skill.displayName = "Test Strike";
            skill.modifier = new SkillModifier
            {
                damagePercent = damagePercent,
                healPercent = 0f,
                accuracyMod = accuracyMod,
                criticalMod = critMod
            };
            skill.targetScope = TargetScope.Enemies;
            skill.useRanks = new List<int> { 1, 2, 3, 4 };
            skill.targetRanks = new List<int> { 1, 2, 3, 4 };
            skill.guaranteedHit = guaranteedHit;
            skill.ignoresDefense = ignoresDefense;
            skill.ignoresDodge = ignoresDodge;
            skill.hitCount = 1;
            skill.maxUsesPerBattle = -1;
            skill.effects = new List<ISkillEffect>();
            return skill;
        }

        /// <summary>
        /// Create a deterministic System.Random with a fixed seed.
        /// </summary>
        public static System.Random CreateFixedRng(int seed = 42)
        {
            return new System.Random(seed);
        }
    }
}
