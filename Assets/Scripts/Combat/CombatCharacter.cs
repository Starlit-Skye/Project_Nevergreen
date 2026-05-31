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
        public int currentHP
        {
            get => _currentHP;
            set
            {
                if (_currentHP != value)
                {
                    _currentHP = value;
                    if (team == Team.Player)
                    {
                        var partyInfo = RunSessionManager.CurrentParty?.Find(p => p.character == this.characterData);
                        if (partyInfo != null)
                        {
                            partyInfo.currentHP = _currentHP;
                        }
                    }
                }
            }
        }
        private int _currentHP;

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
                    if (_state == LifeState.Destroyed || _state == LifeState.Dying)
                    {
                        if (team == Team.Player)
                        {
                            var partyInfo = RunSessionManager.CurrentParty?.Find(p => p.character == this.characterData);
                            if (partyInfo != null)
                            {
                                partyInfo.currentHP = 0;
                            }
                        }
                    }
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
        public bool IsStealthed => statusEffects.Any(s => s.type == StatusType.Stealth && !s.IsExpired);

        /// <summary>
        /// Returns all ranks this character currently occupies, based on anchor rank and size.
        /// A size-1 character at rank 2 returns [2]. A size-2 character at rank 1 returns [1, 2].
        /// </summary>
        public List<int> OccupiedRanks
        {
            get
            {
                int charSize = (characterData != null) ? characterData.size : 1;
                var ranks = new List<int>(charSize);
                for (int i = 0; i < charSize; i++)
                    ranks.Add(rank + i);
                return ranks;
            }
        }

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

            var partyInfo = RunSessionManager.CurrentParty?.Find(p => p.character == this.characterData);
            if (team == Team.Player && partyInfo != null)
            {
                if (partyInfo.currentHP.HasValue)
                {
                    currentHP = partyInfo.currentHP.Value;
                }
                else
                {
                    currentHP = baseStats.maxHP;
                    partyInfo.currentHP = currentHP;
                }
            }
            else
            {
                currentHP = baseStats.maxHP;
            }

            // Equip skills: check RunSessionManager for player-selected skills, fallback to defaults
            equippedSkills.Clear();

            if (team == Team.Player && partyInfo != null && partyInfo.equippedSkills != null && partyInfo.equippedSkills.Count > 0)
            {
                equippedSkills.AddRange(partyInfo.equippedSkills);
            }
            else
            {
                // Fallback to the default CharacterData.availableSkills
                int count = Mathf.Min(4, characterData.availableSkills.Count);
                for (int i = 0; i < count; i++)
                {
                    if (characterData.availableSkills[i] != null)
                        equippedSkills.Add(characterData.availableSkills[i]);
                }
            }

            if (team == Team.Enemy)
            {
                var brain = GetComponent<Nevergreen.Combat.AI.AIBrain>();
                if (brain == null)
                {
                    brain = gameObject.AddComponent<Nevergreen.Combat.AI.AIBrain>();
                }
                brain.profile = characterData.defaultAIProfile;
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
        /// </summary>
        public CombatStats GetEffectiveStats()
        {
            // Track accumulated percentage and flat modifiers for every stat target
            var netPercent = new Dictionary<StatTarget, float>();
            var netFlat = new Dictionary<StatTarget, int>();

            foreach (var status in statusEffects)
            {
                if (status.IsExpired) continue;

                float sign = (status.type == StatusType.Buff) ? 1f : (status.type == StatusType.Debuff) ? -1f : 0f;
                if (sign == 0f) continue;

                // 1. Resolve calculation type dynamically
                var resolvedType = status.amplitudeType;
                if (resolvedType == AmplitudeType.Default)
                {
                    resolvedType = IsFlatStat(status.targetStat) ? AmplitudeType.Flat : AmplitudeType.Percentage;
                }

                // 2. Accumulate in the corresponding dictionary
                if (resolvedType == AmplitudeType.Flat)
                {
                    if (!netFlat.ContainsKey(status.targetStat)) netFlat[status.targetStat] = 0;
                    netFlat[status.targetStat] += Mathf.RoundToInt(sign * status.amplitude);
                }
                else // AmplitudeType.Percentage
                {
                    if (!netPercent.ContainsKey(status.targetStat)) netPercent[status.targetStat] = 0f;
                    netPercent[status.targetStat] += sign * (status.amplitude / 100f);
                }
            }

            CombatStats effective = baseStats.Clone();

            // 3. Apply unified modifications to each of the 12 stats
            effective.attack = GetModifiedStatValue(baseStats.attack, StatTarget.Attack, netPercent, netFlat);
            effective.defense = GetModifiedStatValue(baseStats.defense, StatTarget.Defense, netPercent, netFlat);
            effective.accuracy = GetModifiedStatValue(baseStats.accuracy, StatTarget.Accuracy, netPercent, netFlat);
            effective.dodge = GetModifiedStatValue(baseStats.dodge, StatTarget.Dodge, netPercent, netFlat);
            effective.critChance = GetModifiedStatValue(baseStats.critChance, StatTarget.CritChance, netPercent, netFlat);
            effective.speed = GetModifiedStatValue(baseStats.speed, StatTarget.Speed, netPercent, netFlat);
            effective.maxHP = GetModifiedStatValue(baseStats.maxHP, StatTarget.MaxHP, netPercent, netFlat);
            
            effective.bleedResist = GetModifiedStatValue(baseStats.bleedResist, StatTarget.BleedResist, netPercent, netFlat);
            effective.blightResist = GetModifiedStatValue(baseStats.blightResist, StatTarget.BlightResist, netPercent, netFlat);
            effective.stunResist = GetModifiedStatValue(baseStats.stunResist, StatTarget.StunResist, netPercent, netFlat);
            effective.debuffResist = GetModifiedStatValue(baseStats.debuffResist, StatTarget.DebuffResist, netPercent, netFlat);
            effective.moveResist = GetModifiedStatValue(baseStats.moveResist, StatTarget.MoveResist, netPercent, netFlat);

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
        /// Centralized mathematical logic for stat modifiers.
        /// DESIGNER SPEC: Applies flat additions first, then scales by percentage multipliers.
        /// Formula: (Base + FlatMod) * (1.0 + PercentMod)
        /// </summary>
        private int GetModifiedStatValue(int baseVal, StatTarget target, Dictionary<StatTarget, float> netPercent, Dictionary<StatTarget, int> netFlat)
        {
            float percentMod = netPercent.TryGetValue(target, out float p) ? p : 0f;
            int flatMod = netFlat.TryGetValue(target, out int f) ? f : 0;
            
            return Mathf.RoundToInt((baseVal + flatMod) * (1f + percentMod));
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
        /// Check if a skill can be used from any of this character's occupied ranks.
        /// </summary>
        public bool CanUseSkillFromRank(SkillData skill)
        {
            foreach (int r in OccupiedRanks)
            {
                if (skill.useRanks.Contains(r))
                    return true;
            }
            return false;
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
