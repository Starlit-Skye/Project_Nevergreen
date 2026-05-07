using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Tracks the lifecycle state of a combat entity.
    /// </summary>
    public enum LifeState
    {
        Alive,      // Active participant, takes turns.
        Dying,      // Intermediate state during death animation.
        Pile,       // Spatial anchor, 50% HP, no turns, decay-based.
        Destroyed   // Removed from formation/logic.
    }

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
        [HideInInspector] public int rank;
        [HideInInspector] 
        public LifeState state 
        { 
            get => _state; 
            set 
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke(this, _state);
                }
            }
        }
        private LifeState _state = LifeState.Alive;
        [HideInInspector] public int pileDuration; // Turns remaining before Pile decays
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
        public bool IsAlive => state == LifeState.Alive;
        public bool IsPile => state == LifeState.Pile;
        public bool IsPlayerTeam => team == Team.Player;

        // --- Events ---
        public event Action<CombatCharacter, int> OnDamageTaken; // character, amount
        public event Action<CombatCharacter, int> OnHealed;      // character, amount
        public event Action<CombatCharacter, StatusType, bool> OnStatusApplied;
        public event Action<CombatCharacter, StatusType, int> OnPeriodicEffectApplied;
        public event Action<CombatCharacter, bool> OnDefeated; // character, wasCritical
        public event Action<CombatCharacter> OnStatsChanged;
        public event Action<CombatCharacter, LifeState> OnStateChanged;

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
            state = LifeState.Alive;

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
            // Accumulate modifiers: percentage multipliers for core stats, flat additions for others (resistances/crit)
            Dictionary<StatTarget, float> netPercent = new Dictionary<StatTarget, float>();
            Dictionary<StatTarget, int> netFlat = new Dictionary<StatTarget, int>();

            foreach (var status in statusEffects)
            {
                if (status.IsExpired) continue;

                float sign = (status.type == StatusType.Buff) ? 1f : (status.type == StatusType.Debuff) ? -1f : 0f;
                if (sign == 0f) continue;

                if (IsFlatStat(status.targetStat))
                {
                    if (!netFlat.ContainsKey(status.targetStat))
                        netFlat[status.targetStat] = 0;
                    netFlat[status.targetStat] += Mathf.RoundToInt(sign * status.amplitude);
                }
                else
                {
                    if (!netPercent.ContainsKey(status.targetStat))
                        netPercent[status.targetStat] = 0f;
                    netPercent[status.targetStat] += sign * (status.amplitude / 100f);
                }
            }

            CombatStats effective = baseStats.Clone();

            // Apply percentage multipliers to core stats
            foreach (var kvp in netPercent)
            {
                float multiplier = 1f + kvp.Value;
                ApplyPercentageMultiplier(effective, baseStats, kvp.Key, multiplier);
            }

            // Apply flat additive modifiers
            foreach (var kvp in netFlat)
            {
                ApplyFlatModifier(effective, kvp.Key, kvp.Value);
            }

            // Enforce hard caps
            if (combatConfig != null)
            {
                effective.defense = Mathf.Min(effective.defense, combatConfig.defenseCap);
                effective.dodge = Mathf.Min(effective.dodge, combatConfig.dodgeCap);
            }

            // Apply innate Pile bonuses
            if (state == LifeState.Pile)
            {
                effective.moveResist += 300;
            }

            return effective;
        }

        private bool IsFlatStat(StatTarget target)
        {
            return target == StatTarget.CritChance ||
                   target == StatTarget.BleedResist ||
                   target == StatTarget.BlightResist ||
                   target == StatTarget.StunResist ||
                   target == StatTarget.DebuffResist ||
                   target == StatTarget.MoveResist;
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
                case StatTarget.Speed:       effective.speed = Mathf.RoundToInt(baseStat.speed * multiplier); break;
                case StatTarget.MaxHP:       effective.maxHP = Mathf.RoundToInt(baseStat.maxHP * multiplier); break;
            }
        }

        private void ApplyFlatModifier(CombatStats effective, StatTarget target, int amount)
        {
            switch (target)
            {
                case StatTarget.CritChance:  effective.critChance += amount; break;
                case StatTarget.BleedResist: effective.bleedResist += amount; break;
                case StatTarget.BlightResist:effective.blightResist += amount; break;
                case StatTarget.StunResist:  effective.stunResist += amount; break;
                case StatTarget.DebuffResist:effective.debuffResist += amount; break;
                case StatTarget.MoveResist:  effective.moveResist += amount; break;
            }
        }

        /// <summary>
        /// Apply damage to this character.
        /// </summary>
        public void TakeDamage(int amount, bool isCritical = false)
        {
            if (!IsAlive && !IsPile) return;

            int actual = Mathf.Max(0, amount);
            currentHP = Mathf.Max(0, currentHP - actual);

            OnDamageTaken?.Invoke(this, actual);

            if (currentHP <= 0)
            {
                if (state == LifeState.Pile)
                {
                    state = LifeState.Destroyed;
                }
                else
                {
                    state = LifeState.Dying;
                    OnDefeated?.Invoke(this, isCritical);
                }
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
            status.OnAdded(this);

            if (status.type == StatusType.Stun)
            {
                isStunned = true;
            }

            OnStatsChanged?.Invoke(this);
        }

        public void RemoveStatus(StatusEffectInstance status)
        {
            if (statusEffects.Remove(status))
            {
                status.OnRemoved();
                
                // Re-check stun state if a stun was removed
                if (status.type == StatusType.Stun)
                {
                    isStunned = statusEffects.Any(s => s.type == StatusType.Stun && !s.IsExpired);
                }
                
                TriggerStatsChanged();
            }
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
