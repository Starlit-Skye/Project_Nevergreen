using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Defines one skill usable in combat. Each skill is a unique ScriptableObject.
    /// Skills use the strategy pattern for custom execution effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Nevergreen/Data/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Tooltip("Unique identifier for this skill.")]
        public string skillId;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("Short description of the skill.")]
        [TextArea(2, 4)]
        public string description;

        [Header("Skill Modifier")]
        [Tooltip("Stat-scaling modifiers applied when this skill executes (damage%, heal%, accuracy+, crit+).")]
        public SkillModifier modifier = new SkillModifier();

        [Header("Effects Strategy")]
        [Tooltip("The modular effects executed by this skill.")]
        [SerializeReference]
        [Nevergreen.Attributes.SubclassSelector]
        public List<Nevergreen.Combat.ISkillEffect> effects = new List<Nevergreen.Combat.ISkillEffect>();

        [Header("Rank Constraints")]
        [Tooltip("Which ranks the user must be in to use this skill (1-4).")]
        public List<int> useRanks = new List<int> { 1, 2, 3, 4 };

        [Tooltip("Which ranks this skill can target (1-4).")]
        public List<int> targetRanks = new List<int> { 1, 2, 3, 4 };

        [Header("Targeting")]
        [Tooltip("Who this skill can target.")]
        public TargetScope targetScope = TargetScope.Enemies;

        [Tooltip("Maximum number of targets this skill can hit at once (1-4).")]
        [Range(1, 4)]
        public int maxTargets = 1;

        [Header("Special Flags")]
        [Tooltip("If true, this skill ignores target defense.")]
        public bool ignoresDefense = false;

        [Tooltip("If true, this skill cannot be dodged.")]
        public bool ignoresDodge = false;

        [Tooltip("If true, this skill always hits.")]
        public bool guaranteedHit = false;

        [Tooltip("If true, this skill bypasses guard.")]
        public bool bypassGuard = false;

        [Header("Multi-Hit")]
        [Tooltip("Number of hits this skill performs. Default 1.")]
        [Min(1)]
        public int hitCount = 1;

        [Header("Uses")]
        [Tooltip("Max uses per battle. -1 = unlimited.")]
        public int maxUsesPerBattle = -1;

        [Header("Audio")]
        [Tooltip("Sound effect played when this skill is used.")]
        public AudioClip sfx;
    }

    public enum TargetScope
    {
        Self,
        Allies,
        Enemies
    }

    /// <summary>
    /// An entry describing a status effect a skill can apply.
    /// </summary>
    [Serializable]
    public class SkillStatusEntry
    {
        public StatusType statusType;
        [Tooltip("Which stat this buff/debuff targets. Only used for Buff and Debuff types.")]
        public StatTarget targetStat = StatTarget.Speed;
        [Range(0, 100)]
        public float applicationChance = 100f;
        public int amplitude = 1;
        public int duration = 3;
    }

    public enum StatusType
    {
        Bleed,
        Blight,
        Stun,
        Debuff,
        Mark,
        Buff,
        Guard,
        Riposte,
        Restore,
        Move
    }

    /// <summary>
    /// Which stat a Buff or Debuff status effect modifies.
    /// </summary>
    public enum StatTarget
    {
        Attack,
        Defense,
        Accuracy,
        Dodge,
        CritChance,
        Speed,
        MaxHP,
        BleedResist,
        BlightResist,
        StunResist,
        DebuffResist,
        MoveResist
    }
}
