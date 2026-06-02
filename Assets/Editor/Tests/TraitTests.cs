using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TraitTests
    {
        private CombatCharacter _character;
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
                if (go != null) Object.DestroyImmediate(go);
        }

        private TraitData CreateTrait(string id, TraitType type, TraitEffectStrategy strategy = null)
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.traitId = id;
            trait.displayName = id;
            trait.traitType = type;
            if (strategy != null) trait.effectStrategies.Add(strategy);
            return trait;
        }

        private StatModifierTraitStrategy CreateStatModStrategy(
            StatTarget stat, int amount, AmplitudeType ampType = AmplitudeType.Percentage)
        {
            var strategy = new StatModifierTraitStrategy();
            strategy.targetStat = stat;
            strategy.amount = amount;
            strategy.amplitudeType = ampType;
            return strategy;
        }

        private CombatCharacter CreateCharacterWithTraits(
            List<TraitData> perfections, List<TraitData> imperfections,
            int attack = 100, int defense = 20, int speed = 10, int maxHP = 200)
        {
            var go = new GameObject("TestCharacter_trait");
            var cc = go.AddComponent<CombatCharacter>();
            _cleanup.Add(go);

            var stats = CombatTestHelper.CreateStatBlock(attack, defense, speed: speed, maxHP: maxHP);
            var charData = CombatTestHelper.CreateCharacterData("trait_hero", "TraitHero", stats, CharacterTeamType.Player);

            cc.characterData = charData;
            cc.currentLevel = 1;
            cc.combatConfig = _config;

            // Set up PartyMemberInfo with traits
            var partyInfo = new PartyMemberInfo();
            partyInfo.character = charData;
            partyInfo.perfections = perfections ?? new List<TraitData>();
            partyInfo.imperfections = imperfections ?? new List<TraitData>();
            cc.partyInfo = partyInfo;

            cc.InitializeForCombat(Team.Player, 1);
            return cc;
        }

        // ===== PartyMemberInfo.TryAddTrait tests =====

        [Test]
        public void TryAddTrait_Perfection_Success()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("perf_1", TraitType.Perfection);
            Assert.IsTrue(info.TryAddTrait(trait, _config));
            Assert.AreEqual(1, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_Imperfection_Success()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("imperf_1", TraitType.Imperfection);
            Assert.IsTrue(info.TryAddTrait(trait, _config));
            Assert.AreEqual(1, info.imperfections.Count);
        }

        [Test]
        public void TryAddTrait_DuplicateTraitId_Rejected()
        {
            var info = new PartyMemberInfo();
            var trait1 = CreateTrait("perf_dup", TraitType.Perfection);
            var trait2 = CreateTrait("perf_dup", TraitType.Perfection);
            Assert.IsTrue(info.TryAddTrait(trait1, _config));
            Assert.IsFalse(info.TryAddTrait(trait2, _config), "Duplicate traitId should be rejected.");
            Assert.AreEqual(1, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_ExceedsCapacity_Rejected()
        {
            var info = new PartyMemberInfo();
            for (int i = 0; i < _config.maxPerfections; i++)
            {
                Assert.IsTrue(info.TryAddTrait(CreateTrait($"perf_{i}", TraitType.Perfection), _config));
            }
            Assert.IsFalse(info.TryAddTrait(CreateTrait("perf_overflow", TraitType.Perfection), _config),
                "Should reject when at capacity.");
            Assert.AreEqual(_config.maxPerfections, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_PerfectionsAndImperfections_Independent()
        {
            var info = new PartyMemberInfo();
            Assert.IsTrue(info.TryAddTrait(CreateTrait("perf_1", TraitType.Perfection), _config));
            Assert.IsTrue(info.TryAddTrait(CreateTrait("imperf_1", TraitType.Imperfection), _config));
            Assert.AreEqual(1, info.perfections.Count);
            Assert.AreEqual(1, info.imperfections.Count);
        }

        [Test]
        public void RemoveTrait_Perfection_Success()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("perf_rm", TraitType.Perfection);
            info.TryAddTrait(trait, _config);
            Assert.IsTrue(info.RemoveTrait(trait));
            Assert.AreEqual(0, info.perfections.Count);
        }

        [Test]
        public void RemoveTrait_NotPresent_ReturnsFalse()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("perf_missing", TraitType.Perfection);
            Assert.IsFalse(info.RemoveTrait(trait));
        }

        // ===== StatModifierTraitStrategy tests =====

        [Test]
        public void StatModTrait_PercentageAttackBuff_IncreasesAttack()
        {
            var strategy = CreateStatModStrategy(StatTarget.Attack, 10, AmplitudeType.Percentage);
            var trait = CreateTrait("atk_buff", TraitType.Perfection, strategy);

            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            CombatStats effective = cc.GetEffectiveStats();

            Assert.AreEqual(110, effective.attack, "+10% on base 100 => 110.");
        }

        [Test]
        public void StatModTrait_FlatDefenseDebuff_DecreasesDefense()
        {
            var strategy = CreateStatModStrategy(StatTarget.Defense, -5, AmplitudeType.Flat);
            var trait = CreateTrait("def_debuff", TraitType.Imperfection, strategy);

            var cc = CreateCharacterWithTraits(null, new List<TraitData> { trait }, defense: 20);
            CombatStats effective = cc.GetEffectiveStats();

            Assert.AreEqual(15, effective.defense, "-5 flat on base 20 => 15.");
        }

        [Test]
        public void StatModTrait_StacksWithStatusEffects()
        {
            var strategy = CreateStatModStrategy(StatTarget.Attack, 10, AmplitudeType.Percentage);
            var trait = CreateTrait("atk_buff", TraitType.Perfection, strategy);

            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            // Also add a +20% status buff
            cc.AddStatus(new StatusEffectInstance(StatusType.Buff, StatTarget.Attack, 20, 3));

            CombatStats effective = cc.GetEffectiveStats();
            // Trait adds 10% and status buff adds 20% = net +30% on base 100 => 130
            Assert.AreEqual(130, effective.attack, "Trait +10% and status +20% stack additively => 130.");
        }

        [Test]
        public void StatModTrait_MultipleTraits_StackAdditively()
        {
            var strat1 = CreateStatModStrategy(StatTarget.Attack, 10, AmplitudeType.Percentage);
            var strat2 = CreateStatModStrategy(StatTarget.Attack, 15, AmplitudeType.Percentage);
            var trait1 = CreateTrait("atk_buff_1", TraitType.Perfection, strat1);
            var trait2 = CreateTrait("atk_buff_2", TraitType.Perfection, strat2);

            var cc = CreateCharacterWithTraits(new List<TraitData> { trait1, trait2 }, null, attack: 100);
            CombatStats effective = cc.GetEffectiveStats();

            Assert.AreEqual(125, effective.attack, "+10% + +15% = +25% on base 100 => 125.");
        }

        [Test]
        public void StatModTrait_NullStrategy_NoEffect()
        {
            var trait = CreateTrait("null_strat", TraitType.Perfection, null);
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            CombatStats effective = cc.GetEffectiveStats();
            Assert.AreEqual(100, effective.attack, "Null strategy trait should not change stats.");
        }

        // ===== RankDamageBonusTraitStrategy tests =====

        [Test]
        public void RankDamageTrait_AtRequiredRank_AppliesDamageBonus()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new RankDamageBonusTraitStrategy();
            strategy.requiredRank = 1;
            strategy.damageBonusPercent = 20;

            var trait = CreateTrait("rank_bonus", TraitType.Perfection, strategy);
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            cc.ActivateTraits(battleSystem);
            // Character starts at rank 1

            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(cc, skill, null, battleSystem, CombatTestHelper.CreateFixedRng());
            // Use reflection to invoke the event
            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            Assert.AreEqual(1.2f, ctx.damageMultiplier, 0.001f, "At rank 1: +20% damage bonus.");
        }

        [Test]
        public void RankDamageTrait_NotAtRequiredRank_NoBonus()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new RankDamageBonusTraitStrategy();
            strategy.requiredRank = 3;
            strategy.damageBonusPercent = 20;

            var trait = CreateTrait("rank_bonus", TraitType.Perfection, strategy);
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            cc.ActivateTraits(battleSystem);
            // Character is at rank 1 but bonus requires rank 3

            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(cc, skill, null, battleSystem, CombatTestHelper.CreateFixedRng());
            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            Assert.AreEqual(1.0f, ctx.damageMultiplier, 0.001f, "Not at rank 3: no bonus applied.");
        }

        [Test]
        public void RankDamageTrait_DynamicRankChange_UpdatesCorrectly()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new RankDamageBonusTraitStrategy();
            strategy.requiredRank = 2;
            strategy.damageBonusPercent = 15;

            var trait = CreateTrait("rank_bonus", TraitType.Perfection, strategy);
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            cc.ActivateTraits(battleSystem);

            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // Initially at rank 1 — no bonus
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx1 = new SkillContext(cc, skill, null, battleSystem, CombatTestHelper.CreateFixedRng());
            (eventField.GetValue(battleSystem) as System.Action<SkillContext>)?.Invoke(ctx1);
            Assert.AreEqual(1.0f, ctx1.damageMultiplier, 0.001f, "Rank 1: no bonus.");

            // Move to rank 2
            cc.rank = 2;
            var ctx2 = new SkillContext(cc, skill, null, battleSystem, CombatTestHelper.CreateFixedRng());
            (eventField.GetValue(battleSystem) as System.Action<SkillContext>)?.Invoke(ctx2);
            Assert.AreEqual(1.15f, ctx2.damageMultiplier, 0.001f, "Rank 2: +15% => 1.15.");

            // Move away from rank 2
            cc.rank = 4;
            var ctx3 = new SkillContext(cc, skill, null, battleSystem, CombatTestHelper.CreateFixedRng());
            (eventField.GetValue(battleSystem) as System.Action<SkillContext>)?.Invoke(ctx3);
            Assert.AreEqual(1.0f, ctx3.damageMultiplier, 0.001f, "Rank 4: no bonus.");
        }

        // ===== Lifecycle tests =====

        [Test]
        public void Traits_InitializedFromPartyMemberInfo()
        {
            var strat = CreateStatModStrategy(StatTarget.Speed, 10, AmplitudeType.Flat);
            var perf = CreateTrait("spd_perf", TraitType.Perfection, strat);
            var imperf = CreateTrait("spd_imperf", TraitType.Imperfection);

            var cc = CreateCharacterWithTraits(
                new List<TraitData> { perf },
                new List<TraitData> { imperf });

            Assert.AreEqual(2, cc.activeTraits.Count, "Both perfection and imperfection should be active.");
        }

        [Test]
        public void DeactivateAllTraits_ClearsActiveTraits()
        {
            var strat = CreateStatModStrategy(StatTarget.Attack, 10);
            var trait = CreateTrait("deactivate_test", TraitType.Perfection, strat);

            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            Assert.AreEqual(1, cc.activeTraits.Count);

            cc.DeactivateAllTraits();
            Assert.AreEqual(0, cc.activeTraits.Count);
            Assert.AreEqual(100, cc.GetEffectiveStats().attack, "Stats should return to base after deactivation.");
        }

        [Test]
        public void EnemyCharacters_HaveNoTraits()
        {
            var go = new GameObject("TestEnemy_trait");
            var cc = go.AddComponent<CombatCharacter>();
            _cleanup.Add(go);

            var stats = CombatTestHelper.CreateStatBlock(attack: 50);
            var charData = CombatTestHelper.CreateCharacterData("enemy", "Enemy", stats, CharacterTeamType.Enemy);

            cc.characterData = charData;
            cc.currentLevel = 1;
            cc.combatConfig = _config;
            cc.InitializeForCombat(Team.Enemy, 1);

            Assert.AreEqual(0, cc.activeTraits.Count, "Enemy characters should have no active traits.");
            Assert.AreEqual(50, cc.GetEffectiveStats().attack, "Enemy stats unaffected.");
        }

        [Test]
        public void FlatAndPercentTraits_ApplyCorrectly()
        {
            // Flat +15 Attack and Percent +10% Attack
            var flatStrat = CreateStatModStrategy(StatTarget.Attack, 15, AmplitudeType.Flat);
            var pctStrat = CreateStatModStrategy(StatTarget.Attack, 10, AmplitudeType.Percentage);

            var flatTrait = CreateTrait("flat_atk", TraitType.Perfection, flatStrat);
            var pctTrait = CreateTrait("pct_atk", TraitType.Perfection, pctStrat);

            var cc = CreateCharacterWithTraits(
                new List<TraitData> { flatTrait, pctTrait }, null, attack: 100);

            CombatStats effective = cc.GetEffectiveStats();
            // Flat applied first: 100 + 15 = 115, then percentage: 115 * 1.1 = 126.5 => 126 (banker's rounding)
            Assert.AreEqual(126, effective.attack, "Flat +15 then +10% on base 100 => (100+15)*1.1 = 126.5 => 126.");
        }

        [Test]
        public void TestPerfection_AppliesFlatSpeedPlusTwo()
        {
            var strategy = new Test_Perfection();
            var trait = CreateTrait("test_perf_trait", TraitType.Perfection, strategy);

            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, speed: 10);
            CombatStats effective = cc.GetEffectiveStats();

            Assert.AreEqual(12, effective.speed, "Test_Perfection should add +2 flat speed.");
        }
    }
}
