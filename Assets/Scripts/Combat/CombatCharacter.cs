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
        [HideInInspector] public int currentHP;
        [HideInInspector] public int rank; // 1-4, 1 = front
        [HideInInspector] public Team team;
        [HideInInspector] public CombatStats baseStats;
        [HideInInspector] public List<SkillData> equippedSkills = new List<SkillData>();
        [HideInInspector] public List<StatusEffectInstance> statusEffects = new List<StatusEffectInstance>();
        [HideInInspector] public int actionsPerRound = 1;
        [HideInInspector] public bool isStunned = false;
        [HideInInspector] public int skillUsesThisBattle_count = 0;

        // Track per-skill uses this battle (skill id -> uses)
        private Dictionary<string, int> _skillUseTracker = new Dictionary<string, int>();

        public string DisplayName => characterData != null ? characterData.displayName : gameObject.name;
        public string CharacterId => characterData != null ? characterData.characterId : "";
        public bool IsAlive => currentHP > 0;
        public bool IsPlayerTeam => team == Team.Player;

        // --- Events ---
        public event Action<CombatCharacter, int> OnDamageTaken; // character, amount
        public event Action<CombatCharacter, int> OnHealed;      // character, amount
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
            actionsPerRound = characterData.actionsPerRound;

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
        }

        /// <summary>
        /// Get effective stats after applying all active buff/debuff status effects.
        /// </summary>
        public CombatStats GetEffectiveStats()
        {
            CombatStats effective = baseStats.Clone();

            foreach (var status in statusEffects)
            {
                if (status.IsExpired) continue;

                switch (status.type)
                {
                    case StatusType.Buff:
                        // Buff amplitude adds to speed (generic buff for prototype)
                        effective.speed += status.amplitude;
                        break;
                    case StatusType.Debuff:
                        effective.speed -= status.amplitude;
                        break;
                }
            }

            // Aggregate stun resistance bonus
            // (already handled when stun expires via AddStunRecoveryResist)

            return effective;
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

        /// <summary>
        /// Process start-of-turn status effects (bleed, blight, restore).
        /// </summary>
        public void ProcessStartOfTurnStatuses()
        {
            int totalBleedDamage = 0;
            int totalBlightDamage = 0;
            int totalRestore = 0;

            foreach (var status in statusEffects)
            {
                if (status.IsExpired) continue;

                switch (status.type)
                {
                    case StatusType.Bleed:
                        totalBleedDamage += status.amplitude;
                        break;
                    case StatusType.Blight:
                        totalBlightDamage += status.amplitude;
                        break;
                    case StatusType.Restore:
                        totalRestore += status.amplitude;
                        break;
                }
            }

            // Apply aggregated DOT/HOT
            if (totalBleedDamage > 0) TakeDamage(totalBleedDamage);
            if (totalBlightDamage > 0) TakeDamage(totalBlightDamage);
            if (totalRestore > 0) Heal(totalRestore);
        }

        /// <summary>
        /// Tick all status durations and remove expired ones.
        /// Call at end of this character's turn.
        /// </summary>
        public void TickStatuses(int stunRecoveryResistBonus)
        {
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                statusEffects[i].TickDuration();

                if (statusEffects[i].IsExpired)
                {
                    // If stun just expired, grant stun resist bonus
                    if (statusEffects[i].type == StatusType.Stun)
                    {
                        isStunned = false;
                        baseStats.stunResist += stunRecoveryResistBonus;
                    }

                    statusEffects.RemoveAt(i);
                }
            }

            // Re-check stun state
            isStunned = statusEffects.Any(s => s.type == StatusType.Stun && !s.IsExpired);
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
