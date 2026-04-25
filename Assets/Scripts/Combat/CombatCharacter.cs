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
                        ApplyStatModifier(effective, status.targetStat, status.amplitude);
                        break;
                    case StatusType.Debuff:
                        ApplyStatModifier(effective, status.targetStat, -status.amplitude);
                        break;
                }
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
        /// Apply a signed modifier to the specified stat on a CombatStats instance.
        /// </summary>
        private void ApplyStatModifier(CombatStats stats, StatTarget target, int value)
        {
            switch (target)
            {
                case StatTarget.Attack:      stats.attack += value; break;
                case StatTarget.Defense:     stats.defense += value; break;
                case StatTarget.Accuracy:    stats.accuracy += value; break;
                case StatTarget.Dodge:       stats.dodge += value; break;
                case StatTarget.CritChance:  stats.critChance += value; break;
                case StatTarget.Speed:       stats.speed += value; break;
                case StatTarget.MaxHP:       stats.maxHP += value; break;
                case StatTarget.BleedResist: stats.bleedResist += value; break;
                case StatTarget.BlightResist:stats.blightResist += value; break;
                case StatTarget.StunResist:  stats.stunResist += value; break;
                case StatTarget.DebuffResist:stats.debuffResist += value; break;
                case StatTarget.MoveResist:  stats.moveResist += value; break;
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
