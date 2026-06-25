using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class BuffDebuffTests
    {
        private CombatCharacter _character;
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            CombatTestHelper.InitializeTestDatabase();
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
            _character = CombatTestHelper.CreateCombatCharacter(
                "test_hero", Team.Player, rank: 1,
                attack: 100, defense: 20, speed: 10, maxHP: 200, config: _config);
            _cleanup.Add(_character.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
                if (go != null) Object.DestroyImmediate(go);
                
            if (_config != null)
                ScriptableObject.DestroyImmediate(_config, true);
                
            CombatTestHelper.CleanupTestDatabase();
        }

        [Test]
        public void Buff_Attack_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(110, effective.attack, "10% buff on base 100 => 110.");
        }

        [Test]
        public void Debuff_Attack_DecreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(80, effective.attack, "20% debuff on base 100 => 80.");
        }

        [Test]
        public void Buff_Defense_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Defense, 50, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(30, effective.defense, "50% buff on base 20 => 30.");
        }

        [Test]
        public void Buff_Speed_IncreasesStatByPercentageOfBase()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(12, effective.speed, "20% buff on base 10 => 12.");
        }

        [Test]
        public void MultipleBuffs_SameStat_StackAdditively()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(130, effective.attack, "+10%+20% additive => 130, not 132 compound.");
        }

        [Test]
        public void MultipleDebuffs_SameStat_StackAdditively()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Defense, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Defense, 15, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(15, effective.defense, "-10%-15% additive on base 20 => 15.");
        }

        [Test]
        public void BuffAndDebuff_SameStat_NetToCorrectPercentage()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 30, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 10, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(120, effective.attack, "+30%-10% net +20% on base 100 => 120.");
        }

        [Test]
        public void MultipleBuffs_DifferentStats_ApplyIndependently()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Defense, 50, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(110, effective.attack, "Attack: base 100 * 1.10 = 110.");
            Assert.AreEqual(30, effective.defense, "Defense: base 20 * 1.50 = 30.");
        }

        [Test]
        public void Buff_StunResist_AddsFlatValueToBase()
        {
            _character.baseStats.stunResist = 10;
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.StunResist, 300, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(310, effective.stunResist, "300 flat bonus on base 10 => 310.");
        }

        [Test]
        public void Buff_CritChance_AddsFlatValueToBase()
        {
            _character.baseStats.critChance = 5;
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.CritChance, 10, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(15, effective.critChance, "10 flat bonus on base 5 => 15.");
        }

        [Test]
        public void Debuff_BleedResist_SubtractsFlatValueFromBase()
        {
            _character.baseStats.bleedResist = 40;
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.BleedResist, 20, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(20, effective.bleedResist, "20 flat debuff on base 40 => 20.");
        }

        [Test]
        public void MultipleBuffs_FlatStat_StackAdditively()
        {
            _character.baseStats.critChance = 5;
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.CritChance, 10, 3));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.CritChance, 15, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(30, effective.critChance, "10 and 15 flat buffs on base 5 => 30.");
        }

        [Test]
        public void Debuff_Resistance_CanGoBelowZero()
        {
            _character.baseStats.stunResist = 10;
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.StunResist, 30, 3));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(-20, effective.stunResist, "30 flat debuff on base 10 => -20.");
        }

        [Test]
        public void DebuffResistance_ReducesApplicationChance()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            int applied = 0;
            for (int i = 0; i < 1000; i++)
                if (CombatCalculator.ResolveStatusApplication(100f, 40, rng)) applied++;
            float rate = (float)applied / 1000;
            Assert.That(rate, Is.InRange(0.50f, 0.70f), $"60% effective => ~60%. Got {rate:P1}.");
        }

        [Test]
        public void DebuffResistance_EqualToChance_BlocksAll()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            int applied = 0;
            for (int i = 0; i < 100; i++)
                if (CombatCalculator.ResolveStatusApplication(80f, 80, rng)) applied++;
            Assert.AreEqual(0, applied, "0% effective chance => no applications.");
        }

        [Test]
        public void DebuffResistance_ExceedsChance_BlocksAll()
        {
            var rng = CombatTestHelper.CreateFixedRng(42);
            Assert.IsFalse(CombatCalculator.ResolveStatusApplication(50f, 75, rng),
                "Negative effective chance => never applies.");
        }

        [Test]
        public void BuffDuration_ExpiresAfterCorrectTicks()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 2));
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(1, _character.statusEffects.Count, "Still active after 1 tick.");
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(0, _character.statusEffects.Count, "Removed after 2 ticks.");
            Assert.AreEqual(_character.baseStats.attack, _character.GetEffectiveStats().attack,
                "Stats return to base after expiry.");
        }

        [Test]
        public void DebuffDuration_ExpiresAfterOneTick()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Debuff, StatTarget.Speed, 20, 1));
            StatusProcessor.TickDurations(_character, _config.stunRecoveryResistBonus);
            Assert.AreEqual(0, _character.statusEffects.Count, "1-turn debuff removed after 1 tick.");
        }

        [Test]
        public void ExpiredBuffs_DoNotAffectStats()
        {
            _character.statusEffects.Add(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 50, 0));
            Assert.AreEqual(_character.baseStats.attack, _character.GetEffectiveStats().attack,
                "Expired buff (duration 0) should not modify stats.");
        }

        [Test]
        public void Buff_Attack_FlatModifier()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 15, 3, AmplitudeType.Flat));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(115, effective.attack, "Flat 15 buff on base 100 => 115.");
        }

        [Test]
        public void Buff_StunResist_Percentage()
        {
            _character.baseStats.stunResist = 10;
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.StunResist, 50, 3, AmplitudeType.Percentage));
            CombatStats effective = _character.GetEffectiveStats();
            Assert.AreEqual(15, effective.stunResist, "50% buff on base 10 => 15.");
        }

        [Test]
        public void Buff_Attack_PercentageAndFlat()
        {
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 10, 3, AmplitudeType.Percentage));
            _character.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 15, 3, AmplitudeType.Flat));
            CombatStats effective = _character.GetEffectiveStats();
            // (100 + 15) * 1.1 = 115 * 1.1 = 126.5 => Banker's Rounding (Mathf.RoundToInt) to even = 126
            Assert.AreEqual(126, effective.attack, "Flat applied first (100+15=115), then percentage scaled (115*1.1 = 126.5 => 126 ToEven).");
        }

        [Test]
        public void SelfStatusEffect_AppliesToUser()
        {
            var effect = new SelfStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 20, 
                duration = 2,
                applicationChance = 100f
            };
            
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1);
            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);
            ctx.didHit = true; // Assume a hit

            effect.Execute(ctx, target);

            Assert.AreEqual(1, _character.statusEffects.Count, "Self-buff should be applied to the user.");
            Assert.AreEqual(0, target.statusEffects.Count, "Target should not receive the self-buff.");
            Assert.AreEqual(StatTarget.Speed, _character.statusEffects[0].targetStat);
            Assert.AreEqual(20, _character.statusEffects[0].amplitude);

            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void SelfStatusEffect_MultiHitMultiTarget_AppliesOnlyOnce()
        {
            var effect = new SelfStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Attack, 
                amplitude = 10, 
                duration = 2
            };
            
            var target1 = CombatTestHelper.CreateCombatCharacter("t1", Team.Enemy, 1);
            var target2 = CombatTestHelper.CreateCombatCharacter("t2", Team.Enemy, 2);
            var targets = new List<CombatCharacter> { target1, target2 };
            
            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, targets, null, rng);
            ctx.totalHits = 3;
            ctx.didHit = true;

            // Simulate BattleSystem multi-hit multi-target execution loop
            for (int hit = 0; hit < ctx.totalHits; hit++)
            {
                ctx.currentHitIndex = hit;
                foreach (var t in targets)
                {
                    effect.Execute(ctx, t);
                }
            }

            Assert.AreEqual(1, _character.statusEffects.Count, "Self-buff should only be applied exactly once per skill use despite 3 hits * 2 targets.");
            Assert.AreEqual(10, _character.statusEffects[0].amplitude);

            Object.DestroyImmediate(target1.gameObject);
            Object.DestroyImmediate(target2.gameObject);
        }

        [Test]
        public void SelfStatusEffect_Miss_WithIgnoreMissTrue_AppliesToUser()
        {
            var effect = new SelfStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Defense, 
                amplitude = 30, 
                ignoreMiss = true
            };
            
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1);
            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);
            
            ctx.didHit = false; // Simulate a miss!

            effect.Execute(ctx, target);

            Assert.AreEqual(1, _character.statusEffects.Count, "Self-buff should still apply on a miss if ignoreMiss is true.");

            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void AdjacentAllyStatusEffect_InFront_Size1User_Size1Ally()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.InFront
            };

            _character.rank = 2;

            var ally = CombatTestHelper.CreateCombatCharacter("ally1", Team.Player, rank: 1, size: 1);
            _cleanup.Add(ally.gameObject);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, rank: 1);
            _cleanup.Add(enemy.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { enemy }, null, rng);
            ctx.didHit = true;

            effect.Execute(ctx, enemy);

            Assert.AreEqual(1, ally.statusEffects.Count, "Ally in front should receive the buff.");
            Assert.AreEqual(0, _character.statusEffects.Count, "User should not receive the buff.");
            Assert.AreEqual(15, ally.statusEffects[0].amplitude);
        }

        [Test]
        public void AdjacentAllyStatusEffect_Behind_Size1User_Size2Ally()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.Behind
            };

            _character.rank = 2;

            var ally = CombatTestHelper.CreateCombatCharacter("ally2", Team.Player, rank: 3, size: 2);
            _cleanup.Add(ally.gameObject);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, rank: 1);
            _cleanup.Add(enemy.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { enemy }, null, rng);
            ctx.didHit = true;

            effect.Execute(ctx, enemy);

            Assert.AreEqual(1, ally.statusEffects.Count, "Size 2 ally behind should receive the buff.");
            Assert.AreEqual(15, ally.statusEffects[0].amplitude);
        }

        [Test]
        public void AdjacentAllyStatusEffect_InFront_Size2User_Size1Ally()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.InFront
            };

            _cleanup.Remove(_character.gameObject);
            Object.DestroyImmediate(_character.gameObject);

            var user = CombatTestHelper.CreateCombatCharacter("user", Team.Player, rank: 2, size: 2);
            _cleanup.Add(user.gameObject);

            var ally = CombatTestHelper.CreateCombatCharacter("ally1", Team.Player, rank: 1, size: 1);
            _cleanup.Add(ally.gameObject);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, rank: 1);
            _cleanup.Add(enemy.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { enemy }, null, rng);
            ctx.didHit = true;

            effect.Execute(ctx, enemy);

            Assert.AreEqual(1, ally.statusEffects.Count, "Size 1 ally in front of size 2 user should receive the buff.");
            Assert.AreEqual(15, ally.statusEffects[0].amplitude);
        }

        [Test]
        public void AdjacentAllyStatusEffect_Behind_Size1User_Size3Ally()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.Behind
            };

            _character.rank = 1;

            var ally = CombatTestHelper.CreateCombatCharacter("ally3", Team.Player, rank: 2, size: 3);
            _cleanup.Add(ally.gameObject);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, rank: 1);
            _cleanup.Add(enemy.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { enemy }, null, rng);
            ctx.didHit = true;

            effect.Execute(ctx, enemy);

            Assert.AreEqual(1, ally.statusEffects.Count, "Size 3 ally behind user should receive the buff.");
            Assert.AreEqual(15, ally.statusEffects[0].amplitude);
        }

        [Test]
        public void AdjacentAllyStatusEffect_InFront_Size3User_Size1Ally()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.InFront
            };

            _cleanup.Remove(_character.gameObject);
            Object.DestroyImmediate(_character.gameObject);

            var user = CombatTestHelper.CreateCombatCharacter("user", Team.Player, rank: 2, size: 3);
            _cleanup.Add(user.gameObject);

            var ally = CombatTestHelper.CreateCombatCharacter("ally1", Team.Player, rank: 1, size: 1);
            _cleanup.Add(ally.gameObject);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, rank: 1);
            _cleanup.Add(enemy.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { enemy }, null, rng);
            ctx.didHit = true;

            effect.Execute(ctx, enemy);

            Assert.AreEqual(1, ally.statusEffects.Count, "Size 1 ally in front of size 3 user should receive the buff.");
            Assert.AreEqual(15, ally.statusEffects[0].amplitude);
        }

        [Test]
        public void AdjacentAllyStatusEffect_MultiHitMultiTarget_AppliesOnlyOnce()
        {
            var effect = new AdjacentAllyStatusEffect 
            { 
                statusType = StatusType.Buff, 
                targetStat = StatTarget.Speed, 
                amplitude = 15, 
                duration = 2,
                direction = AllyDirection.InFront
            };

            _character.rank = 2;

            var ally = CombatTestHelper.CreateCombatCharacter("ally1", Team.Player, rank: 1, size: 1);
            _cleanup.Add(ally.gameObject);

            var enemy1 = CombatTestHelper.CreateCombatCharacter("enemy1", Team.Enemy, rank: 1);
            var enemy2 = CombatTestHelper.CreateCombatCharacter("enemy2", Team.Enemy, rank: 2);
            _cleanup.Add(enemy1.gameObject);
            _cleanup.Add(enemy2.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var skill = CombatTestHelper.CreateDamageSkill();
            var targets = new List<CombatCharacter> { enemy1, enemy2 };
            var ctx = new SkillContext(_character, skill, targets, null, rng);
            ctx.totalHits = 3;
            ctx.didHit = true;

            for (int hit = 0; hit < ctx.totalHits; hit++)
            {
                ctx.currentHitIndex = hit;
                foreach (var t in targets)
                {
                    effect.Execute(ctx, t);
                }
            }

            Assert.AreEqual(1, ally.statusEffects.Count, "Adjacent ally should only receive the buff exactly once per skill use.");
        }

        [Test]
        public void StatusEffectOnly_StandaloneHitResolution_SucceedsBasedOnAccuracyAndDodge()
        {
            var effect = new StatusEffect
            {
                statusType = StatusType.Debuff,
                targetStat = StatTarget.Attack,
                amplitude = 10,
                duration = 3,
                applicationChance = 100f,
                ignoreMiss = false
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Enemies;
            skill.effects.Add(effect);

            _character.baseStats.accuracy = 90;

            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, rank: 1);
            target.baseStats.dodge = 10;
            _cleanup.Add(target.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(1);

            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);

            effect.Execute(ctx, target);

            Assert.IsTrue(ctx.hasResolvedHit, "Hit resolution should be executed dynamically.");
            Assert.IsTrue(ctx.didHit, "Hit check should pass based on accuracy vs dodge and RNG.");
            Assert.AreEqual(1, target.statusEffects.Count, "Debuff should apply since the skill hit.");
        }

        [Test]
        public void StatusEffectOnly_StandaloneHitResolution_FailsOnMiss()
        {
            var effect = new StatusEffect
            {
                statusType = StatusType.Debuff,
                targetStat = StatTarget.Attack,
                amplitude = 10,
                duration = 3,
                applicationChance = 100f,
                ignoreMiss = false
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Enemies;
            skill.effects.Add(effect);

            _character.baseStats.accuracy = 50;

            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, rank: 1);
            target.baseStats.dodge = 10;
            _cleanup.Add(target.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(4);

            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);

            effect.Execute(ctx, target);

            Assert.IsTrue(ctx.hasResolvedHit, "Hit resolution should be executed dynamically.");
            Assert.IsFalse(ctx.didHit, "Hit check should fail.");
            Assert.AreEqual(0, target.statusEffects.Count, "Debuff should not apply because the skill missed.");
        }

        [Test]
        public void RemoveStatusEffect_SpecificType_RemovesTargetTypeAndLeavesOthers()
        {
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, rank: 1);
            _cleanup.Add(target.gameObject);

            // Add multiple statuses of different types
            var bleed1 = new StatusEffectInstance(StatusType.Bleed, 2, 3);
            var bleed2 = new StatusEffectInstance(StatusType.Bleed, 3, 3);
            var blight = new StatusEffectInstance(StatusType.Blight, 2, 3);
            var stun = new StatusEffectInstance(StatusType.Stun, 1, 1);

            target.AddStatus(bleed1);
            target.AddStatus(bleed2);
            target.AddStatus(blight);
            target.AddStatus(stun);

            Assert.AreEqual(4, target.statusEffects.Count, "Initial status count is 4.");
            Assert.IsTrue(target.isStunned, "Target is initially stunned.");

            var effect = new RemoveStatusEffect
            {
                removeAll = false,
                targetStatusType = StatusType.Bleed,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);

            effect.Execute(ctx, target);

            Assert.AreEqual(2, target.statusEffects.Count, "Two status effects should remain.");
            Assert.IsFalse(target.statusEffects.Any(s => s.type == StatusType.Bleed), "No Bleed status effects should remain.");
            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Blight), "Blight should remain.");
            Assert.IsTrue(target.statusEffects.Any(s => s.type == StatusType.Stun), "Stun should remain.");
            Assert.IsTrue(target.isStunned, "Target should still be stunned.");
        }

        [Test]
        public void RemoveStatusEffect_RemoveAll_RemovesAllStatusEffects()
        {
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, rank: 1);
            _cleanup.Add(target.gameObject);

            var bleed = new StatusEffectInstance(StatusType.Bleed, 2, 3);
            var blight = new StatusEffectInstance(StatusType.Blight, 2, 3);
            var stun = new StatusEffectInstance(StatusType.Stun, 1, 1);

            target.AddStatus(bleed);
            target.AddStatus(blight);
            target.AddStatus(stun);

            Assert.AreEqual(3, target.statusEffects.Count, "Initial status count is 3.");
            Assert.IsTrue(target.isStunned, "Target is initially stunned.");

            var effect = new RemoveStatusEffect
            {
                removeAll = true,
                ignoreMiss = true
            };

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, null, rng);

            effect.Execute(ctx, target);

            Assert.AreEqual(0, target.statusEffects.Count, "All status effects should be removed.");
            Assert.IsFalse(target.isStunned, "Target stun should be cleared.");
        }

        [Test]
        public void HealReceivedReduction_Debuff_ReducesHealAmount()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1, maxHP: 100);
            _cleanup.Add(target.gameObject);
            target.currentHP = 10; // So we can heal

            // Apply 30% reduction debuff
            var debuff = new HealReceivedDebuffStatusInstance(battleSystem, 30, 3);
            target.AddStatus(debuff);

            var healer = CombatTestHelper.CreateCombatCharacter("healer", Team.Enemy, 2, attack: 50);
            _cleanup.Add(healer.gameObject);

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.skillId = "heal_skill";
            healSkill.modifier = new SkillModifier { healPercent = 1.0f }; // 100% attack scaling
            var healEffect = new HealEffect();
            healSkill.effects.Add(healEffect);

            var ctx = new SkillContext(healer, healSkill, new List<CombatCharacter> { target }, battleSystem, CombatTestHelper.CreateFixedRng());

            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            // Healer has 50 atk, 1.0 multiplier. FixedRng returns a roll that makes base heal = 53.
            // With 30% reduction, heal = 53 * 0.7 = 37.1 -> rounds to 37.
            healEffect.Execute(ctx, target);

            Assert.AreEqual(47, target.currentHP, "HP should increase by 37 (53 base - 30% reduction).");
        }

        [Test]
        public void HealReceivedReduction_Debuff_ChecksDebuffResistance()
        {
            _character.baseStats.debuffResist = 40;
            int resist = _character.GetResistance(StatusType.HealReceivedReduction);
            Assert.AreEqual(40, resist, "HealReceivedReduction should map to debuffResist.");
        }

        [Test]
        public void BleedOnAttack_AppliesBleed_OnAttackHit()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var effect = new BleedOnAttackStatusEffect
            {
                targetSelf = true,
                applicationChance = 100f,
                duration = 3,
                bleedAmplitude = 2,
                bleedDuration = 4,
                bleedChance = 100f
            };

            var skill = CombatTestHelper.CreateDamageSkill();
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, battleSystem, rng);
            ctx.didHit = true; // Assume hit

            // 1. Apply buff to character
            effect.Execute(ctx, target);
            Assert.AreEqual(1, _character.statusEffects.Count, "Character should have BleedOnAttack buff.");

            // 2. Trigger ActionResolved
            var eventField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField?.GetValue(battleSystem) as System.Action<CombatCharacter, SkillData, SkillContext>;
            handler?.Invoke(_character, skill, ctx);

            Assert.AreEqual(1, target.statusEffects.Count, "Target should receive Bleed status.");
            Assert.AreEqual(StatusType.Bleed, target.statusEffects[0].type);
            Assert.AreEqual(2, target.statusEffects[0].amplitude);
            Assert.AreEqual(4, target.statusEffects[0].remainingDuration);
        }

        [Test]
        public void BleedOnAttack_NoBleed_OnAttackMiss()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            _character.AddStatus(new BleedOnAttackStatusInstance(battleSystem, 3, 2, 4, 100f) { Source = _character });

            var skill = CombatTestHelper.CreateDamageSkill();
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1);
            _cleanup.Add(target.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, battleSystem, rng);
            ctx.didHit = false; // MISS

            var eventField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField?.GetValue(battleSystem) as System.Action<CombatCharacter, SkillData, SkillContext>;
            handler?.Invoke(_character, skill, ctx);

            Assert.AreEqual(0, target.statusEffects.Count, "Target should NOT receive Bleed status on a miss.");
        }

        [Test]
        public void BleedOnAttack_RiposteCounter_AppliesBleed()
        {
            var bsGo = new GameObject("BS");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Enemy, 1);
            _cleanup.Add(attacker.gameObject);
            
            var defender = CombatTestHelper.CreateCombatCharacter("Defender", Team.Player, 1);
            _cleanup.Add(defender.gameObject);

            // Need to set max HP higher so they don't die instantly to high damage
            attacker.baseStats.maxHP = 200;
            attacker.currentHP = 200;
            defender.baseStats.maxHP = 200;
            defender.currentHP = 200;

            battleSystem.StartBattle(new List<CombatCharacter> { defender }, new List<CombatCharacter> { attacker });

            // Defender has Riposte and BleedOnAttack
            defender.AddStatus(new StatusEffectInstance(StatusType.Riposte, 100, 3));
            defender.AddStatus(new BleedOnAttackStatusInstance(battleSystem, 3, 2, 4, 100f) { Source = defender });

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.guaranteedHit = true;
            skill.effects.Add(new DamageEffect());

            // Attacker attacks defender
            var method = typeof(BattleSystem).GetMethod("ExecuteSkill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(battleSystem, new object[] { attacker, skill, new List<CombatCharacter> { defender } });

            // Defender should counter attack, which triggers OnActionResolved, applying Bleed to Attacker
            Assert.AreEqual(1, attacker.statusEffects.Count(s => s.type == StatusType.Bleed), "Attacker should receive Bleed from defender's riposte.");
        }

        [Test]
        public void BleedOnAttack_RespectsResistance()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            _character.AddStatus(new BleedOnAttackStatusInstance(battleSystem, 3, 2, 4, 100f) { Source = _character });

            var skill = CombatTestHelper.CreateDamageSkill();
            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1);
            target.baseStats.bleedResist = 100; // 100% resistance
            _cleanup.Add(target.gameObject);

            var rng = CombatTestHelper.CreateFixedRng(42);
            var ctx = new SkillContext(_character, skill, new List<CombatCharacter> { target }, battleSystem, rng);
            ctx.didHit = true;

            var eventField = typeof(BattleSystem).GetField("OnActionResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField?.GetValue(battleSystem) as System.Action<CombatCharacter, SkillData, SkillContext>;
            handler?.Invoke(_character, skill, ctx);

            Assert.AreEqual(0, target.statusEffects.Count, "Target should NOT receive Bleed status because of 100% resistance.");
        }
    }
}
