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
        private GlobalConfig _globalConfig;
        private GameDatabase _gameDb;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
            _globalConfig = CombatTestHelper.CreateDefaultGlobalConfig();
            
            _gameDb = GameDatabase.CreateForTesting(globalCfg: _globalConfig);
            GameDatabase.SetInstanceForTesting(_gameDb);
        }

        [TearDown]
        public void TearDown()
        {
            GameDatabase.SetInstanceForTesting(null);
            
            if (_config != null) ScriptableObject.DestroyImmediate(_config, true);
            if (_globalConfig != null) ScriptableObject.DestroyImmediate(_globalConfig, true);
            
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
            Assert.IsTrue(info.TryAddTrait(trait));
            Assert.AreEqual(1, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_Imperfection_Success()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("imperf_1", TraitType.Imperfection);
            Assert.IsTrue(info.TryAddTrait(trait));
            Assert.AreEqual(1, info.imperfections.Count);
        }

        [Test]
        public void TryAddTrait_DuplicateTraitId_Rejected()
        {
            var info = new PartyMemberInfo();
            var trait1 = CreateTrait("perf_dup", TraitType.Perfection);
            var trait2 = CreateTrait("perf_dup", TraitType.Perfection);
            Assert.IsTrue(info.TryAddTrait(trait1));
            Assert.IsFalse(info.TryAddTrait(trait2), "Duplicate traitId should be rejected.");
            Assert.AreEqual(1, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_ExceedsCapacity_Rejected()
        {
            var info = new PartyMemberInfo();
            for (int i = 0; i < _globalConfig.maxPerfections; i++)
            {
                Assert.IsTrue(info.TryAddTrait(CreateTrait($"perf_{i}", TraitType.Perfection)));
            }
            Assert.IsFalse(info.TryAddTrait(CreateTrait("perf_overflow", TraitType.Perfection)),
                "Should reject when at capacity.");
            Assert.AreEqual(_globalConfig.maxPerfections, info.perfections.Count);
        }

        [Test]
        public void TryAddTrait_PerfectionsAndImperfections_Independent()
        {
            var info = new PartyMemberInfo();
            Assert.IsTrue(info.TryAddTrait(CreateTrait("perf_1", TraitType.Perfection)));
            Assert.IsTrue(info.TryAddTrait(CreateTrait("imperf_1", TraitType.Imperfection)));
            Assert.AreEqual(1, info.perfections.Count);
            Assert.AreEqual(1, info.imperfections.Count);
        }

        [Test]
        public void RemoveTrait_Perfection_Success()
        {
            var info = new PartyMemberInfo();
            var trait = CreateTrait("perf_rm", TraitType.Perfection);
            info.TryAddTrait(trait);
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

        // ===== HealReceivedBonusTraitStrategy tests =====

        [Test]
        public void HealReceivedTrait_IncreasesHealMultiplier_WhenHealedByAlly()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new HealReceivedBonusTraitStrategy();
            strategy.healBonusPercent = 50;

            var trait = CreateTrait("heal_bonus", TraitType.Perfection, strategy);
            
            // The character receiving the heal
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            cc.ActivateTraits(battleSystem);

            // A healer on the same team
            var healer = CreateCharacterWithTraits(null, null, attack: 50);

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.skillId = "heal_skill";
            healSkill.modifier = new SkillModifier { healPercent = 1.0f }; // 100% attack scaling
            healSkill.effects.Add(new HealEffect());

            var ctx = new SkillContext(healer, healSkill, new List<CombatCharacter> { cc }, battleSystem, CombatTestHelper.CreateFixedRng());
            
            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            string key = $"HealReceived_{cc.GetInstanceID()}";
            Assert.IsTrue(ctx.extra.ContainsKey(key), "Context should contain HealReceived bonus key.");
            Assert.AreEqual(0.5f, (float)ctx.extra[key], 0.001f, "Bonus should be 50% (0.5f).");

            cc.currentHP = 10; // Drop HP so we can heal
            healSkill.effects[0].Execute(ctx, cc);

            // Healer has 50 atk, 1.0 multiplier. FixedRng returns a roll that makes base heal = 53.
            // With 50% bonus, heal = 53 * 1.5 = 79.5 -> rounds to 80.
            Assert.AreEqual(90, cc.currentHP, "HP should increase by 80 (53 base + 50% bonus).");
        }

        // ===== StatusApplicationBonusTraitStrategy tests =====
        [Test]
        public void StatusBonusTrait_IncreasesApplicationChance()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new StatusApplicationBonusTraitStrategy();
            strategy.statusType = StatusType.Stun;
            strategy.applicationChanceBonus = 100f; // Huge bonus to guarantee application
            strategy.onlyAgainstEnemies = true;

            var trait = CreateTrait("stun_bonus", TraitType.Perfection, strategy);
            
            var cc = CreateCharacterWithTraits(new List<TraitData> { trait }, null);
            cc.ActivateTraits(battleSystem);

            var enemy = CombatTestHelper.CreateCombatCharacter("enemy", Team.Enemy, 1, maxHP: 100);
            _cleanup.Add(enemy.gameObject);

            var stunSkill = ScriptableObject.CreateInstance<SkillData>();
            stunSkill.skillId = "stun_skill";
            stunSkill.targetScope = TargetScope.Enemies;
            
            var statusEffect = new StatusEffect();
            statusEffect.statusType = StatusType.Stun;
            statusEffect.applicationChance = 0f; // 0% base chance, would never apply normally
            stunSkill.effects.Add(statusEffect);

            var ctx = new SkillContext(cc, stunSkill, new List<CombatCharacter> { enemy }, battleSystem, CombatTestHelper.CreateFixedRng());
            
            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            Assert.IsTrue(ctx.extra.ContainsKey($"StatusChanceBonus_{StatusType.Stun}"), "Context should contain the status chance bonus.");
            Assert.AreEqual(100f, (float)ctx.extra[$"StatusChanceBonus_{StatusType.Stun}"], "Bonus should be exactly 100f.");

            statusEffect.Execute(ctx, enemy);

            bool hasStun = enemy.statusEffects.Exists(s => s.type == StatusType.Stun);
            Assert.IsTrue(hasStun, "Enemy should be stunned due to the 100% bonus from the trait.");
        }
        // ===== HealOutputBonusTraitStrategy tests =====
        [Test]
        public void HealOutputTrait_IncreasesDamageMultiplier_WhenUsingHealSkill()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new HealOutputBonusTraitStrategy();
            strategy.healBonusPercent = 50;

            var trait = CreateTrait("heal_output_bonus", TraitType.Perfection, strategy);
            
            // The character using the heal
            var healer = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 50);
            healer.ActivateTraits(battleSystem);

            var healSkill = ScriptableObject.CreateInstance<SkillData>();
            healSkill.skillId = "heal_skill";
            healSkill.modifier = new SkillModifier { healPercent = 1.0f }; // 100% attack scaling
            
            var ctx = new SkillContext(healer, healSkill, new List<CombatCharacter> { healer }, battleSystem, CombatTestHelper.CreateFixedRng());
            
            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            Assert.AreEqual(1.5f, ctx.damageMultiplier, 0.001f, "Damage multiplier should increase by 50% for healing skill.");
        }
        // ===== FirstRoundStatModifierTraitStrategy tests =====
        [Test]
        public void FirstRoundStatModifierTrait_OnlyActiveInFirstRound()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new FirstRoundStatModifierTraitStrategy();
            strategy.targetStat = StatTarget.Attack;
            strategy.amount = 50; // +50% attack
            strategy.amplitudeType = AmplitudeType.Percentage;

            var trait = CreateTrait("first_round_atk", TraitType.Perfection, strategy);
            
            var character = CreateCharacterWithTraits(new List<TraitData> { trait }, null, attack: 100);
            character.ActivateTraits(battleSystem);

            // Test pre-combat (Round 0)
            var preCombatStats = character.GetEffectiveStats();
            Assert.AreEqual(150, preCombatStats.attack, "Stat buff should be active pre-combat (Round 0).");

            // Test round 1
            typeof(BattleSystem).GetProperty("CurrentRound").SetValue(battleSystem, 1);
            var round1Stats = character.GetEffectiveStats();
            Assert.AreEqual(150, round1Stats.attack, "Stat buff should be active during Round 1.");

            // Test round 2
            typeof(BattleSystem).GetProperty("CurrentRound").SetValue(battleSystem, 2);
            var round2Stats = character.GetEffectiveStats();
            Assert.AreEqual(100, round2Stats.attack, "Stat buff should NOT be active during Round 2.");
        }
        // ===== LowHpStatModifierTraitStrategy tests =====
        [Test]
        public void LowHpStatModifierTrait_OnlyActiveBelowThreshold()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new LowHpStatModifierTraitStrategy();
            strategy.targetStat = StatTarget.Speed;
            strategy.amount = 20; // +20% speed
            strategy.amplitudeType = AmplitudeType.Percentage;
            strategy.hpThresholdPercent = 50f; // Only active at or below 50% HP

            var trait = CreateTrait("low_hp_speed", TraitType.Perfection, strategy);
            
            // Create a character with maxHP = 100
            var character = CreateCharacterWithTraits(new List<TraitData> { trait }, null, speed: 10, maxHP: 100);
            character.ActivateTraits(battleSystem);

            // Test above threshold (100 HP = 100%)
            character.currentHP = 100;
            var aboveThresholdStats = character.GetEffectiveStats();
            Assert.AreEqual(10, aboveThresholdStats.speed, "Speed buff should NOT be active at 100% HP.");

            // Test at exactly threshold (50 HP = 50%)
            character.currentHP = 50;
            var atThresholdStats = character.GetEffectiveStats();
            Assert.AreEqual(12, atThresholdStats.speed, "Speed buff should be active at exactly 50% HP.");

            // Test below threshold (10 HP = 10%)
            character.currentHP = 10;
            var belowThresholdStats = character.GetEffectiveStats();
            Assert.AreEqual(12, belowThresholdStats.speed, "Speed buff should be active below 50% HP.");
        }
        [Test]
        public void LowHpStatModifierTrait_RespectsEffectiveMaxHpBuffs()
        {
            var bsGo = new GameObject("BattleSystem");
            var battleSystem = bsGo.AddComponent<BattleSystem>();
            _cleanup.Add(bsGo);

            var strategy = new LowHpStatModifierTraitStrategy();
            strategy.targetStat = StatTarget.Speed;
            strategy.amount = 50; // +50% speed
            strategy.hpThresholdPercent = 50f; 
            var trait = CreateTrait("low_hp_speed", TraitType.Perfection, strategy);
            
            var character = CreateCharacterWithTraits(new List<TraitData> { trait }, null, speed: 10, maxHP: 100);
            character.ActivateTraits(battleSystem);

            // Initially, maxHP is 100. 50 HP is exactly 50%.
            character.currentHP = 50;
            var initialStats = character.GetEffectiveStats();
            Assert.AreEqual(15, initialStats.speed, "Buff should be active at 50/100 HP (50%).");

            // Apply a buff that increases maxHP by 100 (so effective max HP is 200).
            var hpBuff = new StatusEffectInstance(StatusType.Buff, StatTarget.MaxHP, 100, 1, AmplitudeType.Flat);
            hpBuff.Source = character;
            hpBuff.Host = character;
            character.statusEffects.Add(hpBuff);

            // Now, current HP is still 50. But maxHP is 200.
            // 50 / 200 = 25%, which is < 50%. The trait should STILL be active.
            var buffedStats1 = character.GetEffectiveStats();
            Assert.AreEqual(15, buffedStats1.speed, "Buff should STILL be active at 50/200 HP (25%).");

            // Now, heal character to 120 HP.
            // 120 / 200 = 60%, which is > 50%. The trait should DEACTIVATE.
            character.currentHP = 120;
            var buffedStats2 = character.GetEffectiveStats();
            Assert.AreEqual(10, buffedStats2.speed, "Buff should NOT be active at 120/200 HP (60%).");
        }
        // ===== RankStatModifierTraitStrategy tests =====
        [Test]
        public void RankStatModifierTrait_OnlyActiveInRequiredRank()
        {
            var strategy = new RankStatModifierTraitStrategy();
            strategy.requiredRank = 2; // Must be in Rank 2
            strategy.targetStat = StatTarget.Defense;
            strategy.amount = 15; // +15% Defense
            strategy.amplitudeType = AmplitudeType.Percentage;

            var trait = CreateTrait("rank2_defense", TraitType.Perfection, strategy);
            
            // Create character with base defense 20
            var character = CreateCharacterWithTraits(new List<TraitData> { trait }, null, defense: 20);
            
            // Test at Rank 1 (Not active)
            character.rank = 1;
            var rank1Stats = character.GetEffectiveStats();
            Assert.AreEqual(20, rank1Stats.defense, "Defense should NOT be buffed at rank 1.");

            // Test at Rank 2 (Active)
            character.rank = 2;
            var rank2Stats = character.GetEffectiveStats();
            Assert.AreEqual(23, rank2Stats.defense, "Defense should be buffed (+15%) at rank 2.");

            // Test at Rank 3 (Not active)
            character.rank = 3;
            var rank3Stats = character.GetEffectiveStats();
            Assert.AreEqual(20, rank3Stats.defense, "Defense should NOT be buffed at rank 3.");
        }
    }
}
