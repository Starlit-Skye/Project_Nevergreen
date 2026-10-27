using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.Prototype;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class DotAttackScalingTests
    {
        private CombatConfig _config;
        private List<GameObject> _cleanup;
        private BattleSystem _battleSystem;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();

            var battleGo = new GameObject("BattleSystem");
            _battleSystem = battleGo.AddComponent<BattleSystem>();
            _cleanup.Add(battleGo);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private CombatCharacter CreateCharacter(string id, int attack, int maxHP = 100, int bleedResist = 0, int defense = 0)
        {
            var c = CombatTestHelper.CreateCombatCharacter(id, Team.Player, rank: 1, attack: attack, maxHP: maxHP, config: _config);
            c.characterData.statPerLevel[0].defense = defense;
            c.characterData.statPerLevel[0].bleedResist = bleedResist;
            c.InitializeForCombat(Team.Player, 1);
            _cleanup.Add(c.gameObject);
            return c;
        }

        private SkillContext CreateContext(CombatCharacter user, CombatCharacter target, SkillData skill, System.Random rng = null)
        {
            var targets = target != null ? new List<CombatCharacter> { target } : new List<CombatCharacter>();
            var ctx = new SkillContext(user, skill, targets, _battleSystem, rng ?? new System.Random(42));
            ctx.didHit = true;
            return ctx;
        }

        // --- 1. Resolver Tests ---

        [Test]
        public void Resolver_BaseAttack_CalculatesCorrectly()
        {
            var c = CreateCharacter("c1", attack: 10);
            int damage = StatusInstanceFactory.ResolveDotDamage(c, 30);
            Assert.AreEqual(3, damage, "10 * 30% = 3");
        }

        [Test]
        public void Resolver_MidpointAwayFromZero_RoundsUp()
        {
            var c = CreateCharacter("c2", attack: 10);
            int damage = StatusInstanceFactory.ResolveDotDamage(c, 25);
            Assert.AreEqual(3, damage, "10 * 25% = 2.5, which rounds away from zero to 3");
        }

        [Test]
        public void Resolver_MinClamp_LowAttack_ReturnsAtLeastOne()
        {
            var c = CreateCharacter("c3", attack: 3);
            int damage = StatusInstanceFactory.ResolveDotDamage(c, 10);
            Assert.AreEqual(1, damage, "3 * 10% = 0.3, which clamps to minimum 1");
        }

        [Test]
        public void Resolver_ZeroAttack_StillOne()
        {
            var c = CreateCharacter("c4", attack: 0);
            int damage = StatusInstanceFactory.ResolveDotDamage(c, 50);
            Assert.AreEqual(1, damage, "0 * 50% = 0, which clamps to minimum 1");
        }

        [Test]
        public void Resolver_NullApplier_ReturnsOneWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                int damage = StatusInstanceFactory.ResolveDotDamage(null, 50);
                Assert.AreEqual(1, damage, "Null applier should clamp to 1 without throwing");
            });
        }

        // --- 2. Effective Attack Scaling in Status Application ---

        [Test]
        public void Apply_UsesEffectiveAttack_BuffedApplier()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            // Add +50% attack buff to attacker
            attacker.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 50, 3, AmplitudeType.Percentage));
            Assert.AreEqual(15, attacker.GetEffectiveStats().attack, "Effective attack should be 15");

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "dot_skill_1";
            var effect = new StatusEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 40,
                duration = 3,
                applicationChance = 200f
            };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            var bleed = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(bleed);
            Assert.AreEqual(6, bleed.damagePerTick, "15 * 40% = 6 damage per tick");
        }

        [Test]
        public void Apply_UsesEffectiveAttack_Debuffed()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            // Add -50% attack debuff to attacker
            attacker.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 50, 3, AmplitudeType.Percentage));
            Assert.AreEqual(5, attacker.GetEffectiveStats().attack, "Effective attack should be 5");

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "dot_skill_2";
            var effect = new StatusEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 40,
                duration = 3,
                applicationChance = 200f
            };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            var bleed = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(bleed);
            Assert.AreEqual(2, bleed.damagePerTick, "5 * 40% = 2 damage per tick");
        }

        [Test]
        public void Apply_UsesFlatStatBonus_Applier()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            // Add flat +10 attack buff to attacker
            attacker.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3, AmplitudeType.Flat));
            Assert.AreEqual(20, attacker.GetEffectiveStats().attack, "Effective attack should be 20");

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "dot_skill_flat";
            var effect = new StatusEffect
            {
                statusType = StatusType.Blight,
                amplitude = 30,
                duration = 3,
                applicationChance = 200f
            };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            var blight = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Blight);
            Assert.IsNotNull(blight);
            Assert.AreEqual(6, blight.damagePerTick, "20 * 30% = 6 damage per tick");
        }

        [Test]
        public void Apply_IgnoresSkillScaling()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target1 = CreateCharacter("target1", attack: 10);
            var target2 = CreateCharacter("target2", attack: 10);

            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "s1";
            skill1.modifier = new SkillModifier { damagePercent = 0.5f };
            var effect1 = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill1.effects.Add(effect1);

            var skill2 = ScriptableObject.CreateInstance<SkillData>();
            skill2.skillId = "s2";
            skill2.modifier = new SkillModifier { damagePercent = 3.0f };
            var effect2 = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill2.effects.Add(effect2);

            var ctx1 = CreateContext(attacker, target1, skill1);
            effect1.Execute(ctx1, target1);

            var ctx2 = CreateContext(attacker, target2, skill2);
            effect2.Execute(ctx2, target2);

            var b1 = target1.statusEffects.First(s => s.type == StatusType.Bleed);
            var b2 = target2.statusEffects.First(s => s.type == StatusType.Bleed);

            Assert.AreEqual(3, b1.damagePerTick);
            Assert.AreEqual(3, b2.damagePerTick);
            Assert.AreEqual(b1.damagePerTick, b2.damagePerTick, "DOT scaling must ignore skill damagePercent");
        }

        [Test]
        public void Apply_IgnoresCritAndDamageMultiplier()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "crit_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            ctx.isCritical = true;
            ctx.critMultiplier = 2.0f;
            ctx.damageMultiplier = 3.0f;

            effect.Execute(ctx, target);

            var bleed = target.statusEffects.First(s => s.type == StatusType.Bleed);
            Assert.AreEqual(3, bleed.damagePerTick, "DOT scaling must ignore isCritical and damageMultiplier");
        }

        [Test]
        public void Apply_NoAttackRoll_Deterministic()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "det_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            int[] seeds = { 1, 42, 999, 12345 };
            foreach (var seed in seeds)
            {
                var target = CreateCharacter($"t_{seed}", attack: 10);
                var ctx = CreateContext(attacker, target, skill, new System.Random(seed));
                effect.Execute(ctx, target);
                var bleed = target.statusEffects.First(s => s.type == StatusType.Bleed);
                Assert.AreEqual(3, bleed.damagePerTick, $"Seed {seed} must produce deterministic damagePerTick 3");
            }
        }

        // --- 3. Snapshot & Stacking Tests ---

        [Test]
        public void Snapshot_NotAffectedByLaterBuff()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "snap_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            var bleed = target.statusEffects.First(s => s.type == StatusType.Bleed);
            Assert.AreEqual(3, bleed.damagePerTick);

            // Buff attacker by +200% after status has already been applied
            attacker.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 200, 3, AmplitudeType.Percentage));
            Assert.AreEqual(30, attacker.GetEffectiveStats().attack);

            // Status on target must remain unchanged
            Assert.AreEqual(3, bleed.damagePerTick, "Snapshot damagePerTick must not change when applier is later buffed");
        }

        [Test]
        public void Snapshot_ApplierDies_TicksContinue()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10, maxHP: 100);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "death_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            // Applier dies
            attacker.currentHP = 0;
            attacker.state = LifeState.Dying;

            // Process periodic tick on target
            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(97, target.currentHP, "Target should still take 3 damage from snapshot DOT even if applier is dead");
        }

        [Test]
        public void Stacking_TwoAppliers_SumsBothDamagePerTicks()
        {
            var attackerA = CreateCharacter("attackerA", attack: 10);
            var attackerB = CreateCharacter("attackerB", attack: 20);
            var target = CreateCharacter("target", attack: 10, maxHP: 100);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "stack_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            // Attacker A applies Bleed (10 * 30% = 3)
            var ctxA = CreateContext(attackerA, target, skill);
            effect.Execute(ctxA, target);

            // Attacker B applies Bleed (20 * 30% = 6)
            var ctxB = CreateContext(attackerB, target, skill);
            effect.Execute(ctxB, target);

            Assert.AreEqual(2, target.statusEffects.Count(s => s.type == StatusType.Bleed));

            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(91, target.currentHP, "Target should take 3 + 6 = 9 damage total from both stacks");
        }

        // --- 4. Burn Escalation Tests ---

        [Test]
        public void Burn_EscalatesByOneFlatEachTick()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10, maxHP: 100);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "burn_skill";
            var effect = new StatusEffect { statusType = StatusType.Burn, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            effect.Execute(ctx, target);

            var burn = target.statusEffects.First(s => s.type == StatusType.Burn);
            Assert.AreEqual(3, burn.damagePerTick, "Initial snapshot damagePerTick is 3");

            // Tick 1: escalates to 4, deals 4 dmg
            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(4, burn.damagePerTick);
            Assert.AreEqual(96, target.currentHP);

            // Tick 2: escalates to 5, deals 5 dmg
            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(5, burn.damagePerTick);
            Assert.AreEqual(91, target.currentHP);

            // Tick 3: escalates to 6, deals 6 dmg
            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(6, burn.damagePerTick);
            Assert.AreEqual(85, target.currentHP);
        }

        [Test]
        public void Burn_MultiStack_IncrementsIndependently()
        {
            var target = CreateCharacter("target", attack: 10, maxHP: 100);

            var burn1 = new StatusEffectInstance(StatusType.Burn, StatTarget.MaxHP, 30, 3) { damagePerTick = 3 };
            var burn2 = new StatusEffectInstance(StatusType.Burn, StatTarget.MaxHP, 50, 3) { damagePerTick = 5 };

            target.AddStatus(burn1);
            target.AddStatus(burn2);

            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(4, burn1.damagePerTick);
            Assert.AreEqual(6, burn2.damagePerTick);
            Assert.AreEqual(90, target.currentHP, "First tick deals 4 + 6 = 10 damage (100 - 10 = 90)");
        }

        // --- 5. Burst Trinket Mechanics ---

        [Test]
        public void Burst_DotLump_MultipliesDamagePerTickAndSetsDurationToOne()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "burst_skill";
            var effect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            ctx.extra[$"StatusBurst_{StatusType.Bleed}"] = true;

            effect.Execute(ctx, target);

            var bleed = target.statusEffects.First(s => s.type == StatusType.Bleed);
            Assert.AreEqual(1, bleed.remainingDuration, "Duration must be compressed to 1");
            Assert.AreEqual(9, bleed.damagePerTick, "damagePerTick should be 3 * 3 = 9");
            Assert.AreEqual(30, bleed.amplitude, "Authored amplitude% should remain 30");
        }

        [Test]
        public void Burst_NonDot_Unchanged()
        {
            var attacker = CreateCharacter("attacker", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "burst_debuff_skill";
            var effect = new StatusEffect { statusType = StatusType.Debuff, targetStat = StatTarget.Speed, amplitude = 10, duration = 3, applicationChance = 200f };
            skill.effects.Add(effect);

            var ctx = CreateContext(attacker, target, skill);
            ctx.extra[$"StatusBurst_{StatusType.Debuff}"] = true;

            effect.Execute(ctx, target);

            var debuff = target.statusEffects.First(s => s.type == StatusType.Debuff);
            Assert.AreEqual(1, debuff.remainingDuration, "Duration must be compressed to 1");
            Assert.AreEqual(30, debuff.amplitude, "Non-DOT amplitude must be multiplied to 30");
        }

        // --- 6. BleedOnAttack Status Instance ---

        [Test]
        public void BleedOnAttack_UsesHostAttack()
        {
            var host = CreateCharacter("host", attack: 10);
            var target = CreateCharacter("target", attack: 10);

            var buff = new BleedOnAttackStatusInstance(_battleSystem, duration: 3, bleedAmplitude: 30, bleedDuration: 3, bleedChance: 200f);
            host.AddStatus(buff);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "attack_skill";
            skill.modifier = new SkillModifier { damagePercent = 1.0f };

            var ctx = CreateContext(host, target, skill);

            // Raise OnActionResolved directly on the battle system to simulate action completion
            var ev = _battleSystem.GetType().GetField("OnActionResolved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var del = (Action<CombatCharacter, SkillData, SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(host, skill, ctx);

            var bleed = target.statusEffects.FirstOrDefault(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(bleed, "Bleed should be applied on hit");
            Assert.AreEqual(3, bleed.damagePerTick, "Host attack 10 * 30% = 3 dmg/tick");
            Assert.AreEqual(host, bleed.Source, "Bleed Source must be Host");
            Assert.AreEqual(StatTarget.Speed, bleed.targetStat);
            Assert.AreEqual(30, bleed.amplitude);
        }

        [Test]
        public void BleedOnAttack_ResistanceStillApplies()
        {
            var host = CreateCharacter("host", attack: 10);
            var target = CreateCharacter("target", attack: 10, bleedResist: 300);

            var buff = new BleedOnAttackStatusInstance(_battleSystem, duration: 3, bleedAmplitude: 30, bleedDuration: 3, bleedChance: 100f);
            host.AddStatus(buff);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "attack_skill_resist";
            skill.modifier = new SkillModifier { damagePercent = 1.0f };

            var ctx = CreateContext(host, target, skill);

            bool statusAppliedFired = false;
            bool statusAppliedSucceeded = true;
            target.OnStatusApplied += (c, type, succeeded, stat) =>
            {
                if (type == StatusType.Bleed)
                {
                    statusAppliedFired = true;
                    statusAppliedSucceeded = succeeded;
                }
            };

            var ev = _battleSystem.GetType().GetField("OnActionResolved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var del = (Action<CombatCharacter, SkillData, SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(host, skill, ctx);

            Assert.IsFalse(target.HasActiveStatus(StatusType.Bleed), "Target with 300 bleed resist must not gain Bleed");
            Assert.IsTrue(statusAppliedFired, "OnStatusApplied event should fire");
            Assert.IsFalse(statusAppliedSucceeded, "OnStatusApplied should report succeeded = false");
        }

        [Test]
        public void BleedOnAttack_ParityWithStatusEffect()
        {
            var host = CreateCharacter("host", attack: 10);
            var targetA = CreateCharacter("targetA", attack: 10);
            var targetB = CreateCharacter("targetB", attack: 10);

            // 1. StatusEffect direct application
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "parity_skill";
            skill.modifier = new SkillModifier { damagePercent = 1.0f };
            var directEffect = new StatusEffect { statusType = StatusType.Bleed, amplitude = 30, duration = 3, applicationChance = 200f };
            skill.effects.Add(directEffect);
            var ctxA = CreateContext(host, targetA, skill);
            directEffect.Execute(ctxA, targetA);

            // 2. BleedOnAttack proc
            var buff = new BleedOnAttackStatusInstance(_battleSystem, duration: 3, bleedAmplitude: 30, bleedDuration: 3, bleedChance: 200f);
            host.AddStatus(buff);
            var ctxB = CreateContext(host, targetB, skill);
            var ev = _battleSystem.GetType().GetField("OnActionResolved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var del = (Action<CombatCharacter, SkillData, SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(host, skill, ctxB);

            var bleedDirect = targetA.statusEffects.First(s => s.type == StatusType.Bleed);
            var bleedProc = targetB.statusEffects.First(s => s.type == StatusType.Bleed);

            Assert.AreEqual(bleedDirect.damagePerTick, bleedProc.damagePerTick, "Both pathways must produce identical damagePerTick");
        }

        // --- 7. Defense and Restore Checks ---

        [Test]
        public void Restore_StillFlat_IgnoresAttack()
        {
            var healer = CreateCharacter("healer", attack: 100);
            var target = CreateCharacter("target", attack: 10, maxHP: 100);
            target.currentHP = 50;

            // Add Restore with amplitude 15
            target.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 15, 3));

            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(65, target.currentHP, "Restore heals by flat amplitude (50 + 15 = 65) ignoring character attack");
        }

        [Test]
        public void Tick_IgnoresDefense()
        {
            var target = CreateCharacter("target", attack: 10, maxHP: 100, defense: 50);
            target.AddStatus(new StatusEffectInstance(StatusType.Bleed, StatTarget.MaxHP, 30, 3) { damagePerTick = 10 });

            StatusProcessor.ProcessPeriodicEffects(target);
            Assert.AreEqual(90, target.currentHP, "DoT ticks must bypass 50% defense and deal full 10 damage");
        }

        // --- 8. Tooltip Tests ---

        [Test]
        public void Tooltip_ShowsResolvedDamage()
        {
            var hpBarGO = new GameObject("HPBar");
            var hpBar = hpBarGO.AddComponent<HPBar>();
            var tooltipDisplay = hpBarGO.AddComponent<StatusTooltipDisplay>();

            var textGO = new GameObject("TooltipText");
            var tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            var textField = typeof(StatusTooltipDisplay).GetField("tooltipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textField.SetValue(tooltipDisplay, tooltipText);

            var onEnableMethod = typeof(StatusTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(tooltipDisplay, null);

            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(hpBarGO.transform, false);
            var trigger = iconGO.AddComponent<StatusIconTooltipTrigger>();

            var status = new StatusEffectInstance(StatusType.Bleed, StatTarget.Speed, 30, 2) { damagePerTick = 7 };
            trigger.Initialize(status);
            trigger.OnPointerEnter(null);

            Assert.AreEqual("7 dmg for 2 turns", tooltipText.text);

            var onDisableMethod = typeof(StatusTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(tooltipDisplay, null);

            UnityEngine.Object.DestroyImmediate(tooltipDisplay);
            UnityEngine.Object.DestroyImmediate(textGO);
            UnityEngine.Object.DestroyImmediate(iconGO);
            UnityEngine.Object.DestroyImmediate(hpBarGO);
        }

        [Test]
        public void Tooltip_AggregatesDamage()
        {
            var hpBarGO = new GameObject("HPBar");
            var hpBar = hpBarGO.AddComponent<HPBar>();
            var tooltipDisplay = hpBarGO.AddComponent<StatusTooltipDisplay>();

            var textGO = new GameObject("TooltipText");
            var tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            var textField = typeof(StatusTooltipDisplay).GetField("tooltipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textField.SetValue(tooltipDisplay, tooltipText);

            var onEnableMethod = typeof(StatusTooltipDisplay).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod.Invoke(tooltipDisplay, null);

            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(hpBarGO.transform, false);
            var trigger = iconGO.AddComponent<StatusIconTooltipTrigger>();

            var character = CreateCharacter("test_char", attack: 10);
            hpBar.Initialize(character, null);

            var bleed1 = new StatusEffectInstance(StatusType.Bleed, StatTarget.Speed, 20, 3) { damagePerTick = 4 };
            var bleed2 = new StatusEffectInstance(StatusType.Bleed, StatTarget.Speed, 30, 2) { damagePerTick = 3 };

            character.AddStatus(bleed1);
            character.AddStatus(bleed2);

            trigger.Initialize(bleed1);
            trigger.OnPointerEnter(null);

            Assert.AreEqual("7 dmg for 3 turns", tooltipText.text, "Tooltip should aggregate damagePerTick: 4 + 3 = 7");

            var onDisableMethod = typeof(StatusTooltipDisplay).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisableMethod.Invoke(tooltipDisplay, null);

            UnityEngine.Object.DestroyImmediate(tooltipDisplay);
            UnityEngine.Object.DestroyImmediate(textGO);
            UnityEngine.Object.DestroyImmediate(iconGO);
            UnityEngine.Object.DestroyImmediate(hpBarGO);
        }

        // --- 9. Production Code Scan Test ---

        [Test]
        public void NoDirectDotConstruction_InProductionCode()
        {
            string scriptsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "Scripts"));
            Assert.IsTrue(Directory.Exists(scriptsDir), "Assets/Scripts directory should exist");

            var csFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
            var regex = new Regex(@"new\s+StatusEffectInstance\s*\(\s*StatusType\.(Bleed|Blight|Burn)");

            var violations = new List<string>();
            foreach (var file in csFiles)
            {
                string content = File.ReadAllText(file);
                var matches = regex.Matches(content);
                if (matches.Count > 0)
                {
                    violations.Add($"{Path.GetFileName(file)} ({matches.Count} matches)");
                }
            }

            Assert.IsEmpty(violations, $"Found direct DOT constructions in production code: {string.Join(", ", violations)}. All DOT creation must go through StatusInstanceFactory.");
        }
    }
}
