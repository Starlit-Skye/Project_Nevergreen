using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime combat entity. Attached to each character prefab in the combat scene.
    /// Holds current HP, resolved stats, rank, skills, and active status effects.
    /// </summary>
    public class CombatCharacter : MonoBehaviour
    {
        [Header("Character Setup")]
        [Tooltip("Character definition data asset.")]
        public CharacterData characterData;

        [Tooltip("Current level of this character.")]
        [Min(1)]
        public int currentLevel = 1;

        // --- Runtime State (set during combat setup) ---
         public int currentHP;
        [HideInInspector] public int rank; // 1-4, 1 = front
        [HideInInspector] public Team team;
        [HideInInspector] public CombatConfig combatConfig;
        [HideInInspector] public CombatStats baseStats;
        [HideInInspector] public List<SkillData> equippedSkills = new List<SkillData>();
        [HideInInspector] public List<StatusEffectInstance> statusEffects = new List<StatusEffectInstance>();

        [HideInInspector] public bool isStunned = false;
        [HideInInspector] public int skillUsesThisBattle_count = 0;

        public Animator animator { get; private set; }

        // Track per-skill uses this battle (skill id -> uses)
        private Dictionary<string, int> _skillUseTracker = new Dictionary<string, int>();

        public string DisplayName => characterData != null ? characterData.displayName : gameObject.name;
        public string CharacterId => characterData != null ? characterData.characterId : "";
        public bool IsAlive => currentHP > 0;
        public bool IsPlayerTeam => team == Team.Player;

        // --- Events ---
        public event Action<CombatCharacter, int> OnDamageTaken; // character, amount
        public event Action<CombatCharacter, int> OnHealed;      // character, amount
        public event Action<CombatCharacter, StatusType, bool> OnStatusApplied;
        public event Action<CombatCharacter, StatusType, int> OnPeriodicEffectApplied;
        public event Action<CombatCharacter> OnDefeated;
        public event Action<CombatCharacter> OnStatsChanged;

        /// <summary>
        /// Initialize this character for combat from its CharacterData.
        /// Called by BattleSystem during combat setup.
        /// </summary>
        public void InitializeForCombat(Team assignedTeam, int assignedRank)
        {
            if (characterData == null)
            {
                Debug.LogError($"[CombatCharacter] No CharacterData assigned to {gameObject.name}");
                return;
            }

            team = assignedTeam;
            rank = assignedRank;


            // Resolve stats from level
            StatBlockData statBlock = characterData.GetStatsForLevel(currentLevel);
            if (statBlock == null) return;

            baseStats = new CombatStats(statBlock);
            currentHP = baseStats.maxHP;

            // Equip skills (up to 4 from available)
            equippedSkills.Clear();
            int count = Mathf.Min(4, characterData.availableSkills.Count);
            for (int i = 0; i < count; i++)
            {
                if (characterData.availableSkills[i] != null)
                    equippedSkills.Add(characterData.availableSkills[i]);
            }

            // Reset status tracking
            statusEffects.Clear();
            _skillUseTracker.Clear();
            isStunned = false;

            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[CombatCharacter] {DisplayName} - No Animator found in children! Animations will be skipped.");
            }
        }

        /// <summary>
        /// Get effective stats after applying all active buff/debuff status effects.
        /// Buffs/Debuffs are percentage multipliers of the base stat, stacked additively.
        /// Formula: effective = round(base * (1.0 + sum(buff_amplitudes/100) - sum(debuff_amplitudes/100)))
        /// </summary>
        public CombatStats GetEffectiveStats()
        {
            // Accumulate net percentage modifier per stat target
            Dictionary<StatTarget, float> netPercent = new Dictionary<StatTarget, float>();

            foreach (var status in statusEffects)
            {
                if (status.IsExpired) continue;

                float sign;
                if (status.type == StatusType.Buff)
                    sign = 1f;
                else if (status.type == StatusType.Debuff)
                    sign = -1f;
                else
                    continue;

                if (!netPercent.ContainsKey(status.targetStat))
                    netPercent[status.targetStat] = 0f;

                netPercent[status.targetStat] += sign * (status.amplitude / 100f);
            }

            CombatStats effective = baseStats.Clone();

            // Apply each accumulated multiplier to the base stat
            foreach (var kvp in netPercent)
            {
                float multiplier = 1f + kvp.Value;
                ApplyPercentageMultiplier(effective, baseStats, kvp.Key, multiplier);
            }

            // Enforce hard caps
            if (combatConfig != null)
            {
                effective.defense = Mathf.Min(effective.defense, combatConfig.defenseCap);
                effective.dodge = Mathf.Min(effective.dodge, combatConfig.dodgeCap);
            }

            return effective;
        }

        /// <summary>
        /// Apply a percentage multiplier to the specified stat, using the base stat as reference.
        /// Result is rounded to nearest integer.
        /// </summary>
        private void ApplyPercentageMultiplier(CombatStats effective, CombatStats baseStat,
                                                StatTarget target, float multiplier)
        {
            switch (target)
            {
                case StatTarget.Attack:      effective.attack = Mathf.RoundToInt(baseStat.attack * multiplier); break;
                case StatTarget.Defense:     effective.defense = Mathf.RoundToInt(baseStat.defense * multiplier); break;
                case StatTarget.Accuracy:    effective.accuracy = Mathf.RoundToInt(baseStat.accuracy * multiplier); break;
                case StatTarget.Dodge:       effective.dodge = Mathf.RoundToInt(baseStat.dodge * multiplier); break;
                case StatTarget.CritChance:  effective.critChance = Mathf.RoundToInt(baseStat.critChance * multiplier); break;
                case StatTarget.Speed:       effective.speed = Mathf.RoundToInt(baseStat.speed * multiplier); break;
                case StatTarget.MaxHP:       effective.maxHP = Mathf.RoundToInt(baseStat.maxHP * multiplier); break;
                case StatTarget.BleedResist: effective.bleedResist = Mathf.RoundToInt(baseStat.bleedResist * multiplier); break;
                case StatTarget.BlightResist:effective.blightResist = Mathf.RoundToInt(baseStat.blightResist * multiplier); break;
                case StatTarget.StunResist:  effective.stunResist = Mathf.RoundToInt(baseStat.stunResist * multiplier); break;
                case StatTarget.DebuffResist:effective.debuffResist = Mathf.RoundToInt(baseStat.debuffResist * multiplier); break;
                case StatTarget.MoveResist:  effective.moveResist = Mathf.RoundToInt(baseStat.moveResist * multiplier); break;
            }
        }

        /// <summary>
        /// Apply damage to this character.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;

            int actual = Mathf.Max(0, amount);
            currentHP = Mathf.Max(0, currentHP - actual);

            OnDamageTaken?.Invoke(this, actual);

            if (!IsAlive)
            {
                OnDefeated?.Invoke(this);
            }
        }

        /// <summary>
        /// Heal this character.
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;

            int actual = Mathf.Min(amount, baseStats.maxHP - currentHP);
            currentHP += actual;

            OnHealed?.Invoke(this, actual);
        }

        /// <summary>
        /// Add a status effect to this character.
        /// </summary>
        public void AddStatus(StatusEffectInstance status)
        {
            statusEffects.Add(status);

            if (status.type == StatusType.Stun)
            {
                isStunned = true;
            }

            OnStatsChanged?.Invoke(this);
        }

        public void TriggerStatusApplied(StatusType type, bool succeeded)
        {
            OnStatusApplied?.Invoke(this, type, succeeded);
        }

        public void TriggerPeriodicEffectApplied(StatusType type, int amount)
        {
            OnPeriodicEffectApplied?.Invoke(this, type, amount);
        }

        public void TriggerStatsChanged()
        {
            OnStatsChanged?.Invoke(this);
        }

        /// <summary>
        /// Check if a skill can be used from the current rank.
        /// </summary>
        public bool CanUseSkillFromRank(SkillData skill)
        {
            return skill.useRanks.Contains(rank);
        }

        /// <summary>
        /// Check if a skill has remaining uses this battle.
        /// </summary>
        public bool HasRemainingUses(SkillData skill)
        {
            if (skill.maxUsesPerBattle < 0) return true; // unlimited
            _skillUseTracker.TryGetValue(skill.skillId, out int used);
            return used < skill.maxUsesPerBattle;
        }

        /// <summary>
        /// Record a use of a skill for per-battle tracking.
        /// </summary>
        public void RecordSkillUse(SkillData skill)
        {
            if (!_skillUseTracker.ContainsKey(skill.skillId))
                _skillUseTracker[skill.skillId] = 0;
            _skillUseTracker[skill.skillId]++;
        }

        /// <summary>
        /// Get the resistance value for a given status type.
        /// </summary>
        public int GetResistance(StatusType type)
        {
            CombatStats eff = GetEffectiveStats();
            return type switch
            {
                StatusType.Bleed => eff.bleedResist,
                StatusType.Blight => eff.blightResist,
                StatusType.Stun => eff.stunResist,
                StatusType.Debuff => eff.debuffResist,
                StatusType.Move => eff.moveResist,
                _ => 0
            };
        }
    }

    public enum Team
    {
        Player,
        Enemy
    }
}
