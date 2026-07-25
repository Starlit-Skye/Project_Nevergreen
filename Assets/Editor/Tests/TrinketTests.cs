using System.Collections.Generic;
using Nevergreen.Combat;
using Nevergreen.Data;
using NUnit.Framework;
using UnityEngine;

namespace Nevergreen.Tests
{
    public class TrinketTests
    {
        private PartyMemberInfo _partyMember;
        private CombatCharacter _character;
        private BattleSystem _battleSystem;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("TestCharacter");
            _character = go.AddComponent<CombatCharacter>();
            
            var battleGo = new GameObject("BattleSystem");
            _battleSystem = battleGo.AddComponent<BattleSystem>();

            var charData = ScriptableObject.CreateInstance<CharacterData>();
            var stats = ScriptableObject.CreateInstance<StatBlockData>();
            stats.maxHP = 100;
            stats.attack = 10;
            charData.statPerLevel.Add(stats);
            _character.characterData = charData;

            _partyMember = new PartyMemberInfo
            {
                character = charData,
                currentLevel = 1,
                currentHP = 100
            };
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_character.gameObject);
            Object.DestroyImmediate(_battleSystem.gameObject);
        }

        [Test]
        public void TryEquipTrinket_EnforcesMaxCapacityOfTwo()
        {
            var t1 = ScriptableObject.CreateInstance<TrinketData>(); t1.trinketId = "t1";
            var t2 = ScriptableObject.CreateInstance<TrinketData>(); t2.trinketId = "t2";
            var t3 = ScriptableObject.CreateInstance<TrinketData>(); t3.trinketId = "t3";

            Assert.IsTrue(_partyMember.TryEquipTrinket(t1));
            Assert.IsTrue(_partyMember.TryEquipTrinket(t2));
            Assert.IsFalse(_partyMember.TryEquipTrinket(t3)); // Capacity reached

            Assert.AreEqual(2, _partyMember.equippedTrinkets.Count);
        }

        [Test]
        public void TryEquipTrinket_EnforcesUniqueness()
        {
            var t1 = ScriptableObject.CreateInstance<TrinketData>(); t1.trinketId = "t1";
            var t2 = ScriptableObject.CreateInstance<TrinketData>(); t2.trinketId = "t1"; // duplicate ID

            Assert.IsTrue(_partyMember.TryEquipTrinket(t1));
            Assert.IsFalse(_partyMember.TryEquipTrinket(t2)); // Already equipped

            Assert.AreEqual(1, _partyMember.equippedTrinkets.Count);
        }

        [Test]
        public void TryUnequipTrinket_PreventsRemovalIfCursed()
        {
            var cursedTrinket = ScriptableObject.CreateInstance<TrinketData>();
            cursedTrinket.trinketId = "cursed";
            cursedTrinket.cannotBeRemoved = true;

            var normalTrinket = ScriptableObject.CreateInstance<TrinketData>();
            normalTrinket.trinketId = "normal";

            _partyMember.TryEquipTrinket(cursedTrinket);
            _partyMember.TryEquipTrinket(normalTrinket);

            Assert.IsFalse(_partyMember.TryUnequipTrinket(cursedTrinket));
            Assert.IsTrue(_partyMember.TryUnequipTrinket(normalTrinket));
        }

        [Test]
        public void StatModifierTrinket_AppliesCorrectStatsInCombat()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.trinketId = "stat_up";
            
            var flatStrat = new StatModifierTrinketStrategy
            {
                statTarget = StatTarget.Attack,
                amplitudeType = AmplitudeType.Flat,
                amount = 5f
            };
            var percentStrat = new StatModifierTrinketStrategy
            {
                statTarget = StatTarget.Attack,
                amplitudeType = AmplitudeType.Percentage,
                amount = 50f // +50%
            };
            trinket.effectStrategies.Add(flatStrat);
            trinket.effectStrategies.Add(percentStrat);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);

            // Base attack is 10.
            // Expected: (10 + 5) * 1.5 = 22.5 => 22 (banker's rounding)
            var stats = _character.GetEffectiveStats();
            Assert.AreEqual(22, stats.attack);
        }

        [Test]
        public void GuaranteedHitTrinket_MutatesSkillContextOnDamageCalc()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.effectStrategies.Add(new GuaranteedHitTrinketStrategy());

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem); // Hooks events

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var targets = new List<CombatCharacter> { _character }; // Target self for test
            var rng = new System.Random();
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);

            // Simulate the event invoke manually because we are isolating it
            ctx.guaranteedHit = false; // base
            
            // Replicate BattleSystem emission
            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            Assert.IsTrue(ctx.guaranteedHit);
        }

        [Test]
        public void HealOutputBonusTrinket_IncreasesDamageMultiplierForHealSkills()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new HealOutputBonusTrinketStrategy { healBonusPercent = 30 };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { healPercent = 15f };

            var targets = new List<CombatCharacter> { _character };
            var rng = new System.Random();
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);
            ctx.damageMultiplier = 1f;

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            Assert.AreEqual(1.3f, ctx.damageMultiplier);
        }

        [Test]
        public void HealReceivedBonusTrinket_AddsHealReceivedBonusToContext()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new HealReceivedBonusTrinketStrategy { healBonusPercent = 25 };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            // Create ally
            var allyGo = new GameObject("AllyCharacter");
            var ally = allyGo.AddComponent<CombatCharacter>();
            ally.team = Team.Player;

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { healPercent = 10f };

            var targets = new List<CombatCharacter> { _character };
            var rng = new System.Random();
            var ctx = new SkillContext(ally, skill, targets, _battleSystem, rng);

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            string key = $"HealReceived_{_character.GetInstanceID()}";
            Assert.IsTrue(ctx.extra.ContainsKey(key));
            Assert.AreEqual(0.25f, (float)ctx.extra[key]);

            Object.DestroyImmediate(allyGo);
        }

        [Test]
        public void StatusApplicationBonusTrinket_AddsStatusChanceBonusToContext()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new StatusApplicationBonusTrinketStrategy 
            { 
                statusType = StatusType.Stun, 
                applicationChanceBonus = 15f,
                onlyAgainstEnemies = true
            };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Enemies;

            var targets = new List<CombatCharacter> { _character };
            var rng = new System.Random();
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            string key = "StatusChanceBonus_Stun";
            Assert.IsTrue(ctx.extra.ContainsKey(key));
            Assert.AreEqual(15f, (float)ctx.extra[key]);
        }

        [Test]
        public void CritDamageMultiplierBonusTrinket_IncreasesCriticalDamageMultiplierInDamageCalculation()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new CritDamageMultiplierBonusTrinketStrategy { critMultiplierBonus = 0.5f };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { damagePercent = 1f }; // 100% damage

            // Create 2 targets for AOE simulation, both with 0 defense
            var enemyGo1 = new GameObject("Enemy1");
            var enemy1 = enemyGo1.AddComponent<CombatCharacter>();
            var enemyData = ScriptableObject.CreateInstance<CharacterData>();
            var enemyStats = ScriptableObject.CreateInstance<StatBlockData>();
            enemyStats.defense = 0; 
            enemyData.statPerLevel.Add(enemyStats);
            enemy1.characterData = enemyData;
            enemy1.InitializeForCombat(Team.Enemy, 1);

            var enemyGo2 = new GameObject("Enemy2");
            var enemy2 = enemyGo2.AddComponent<CombatCharacter>();
            enemy2.characterData = enemyData; // reuse data
            enemy2.InitializeForCombat(Team.Enemy, 2);

            var targets = new List<CombatCharacter> { enemy1, enemy2 };
            var rng = new System.Random(42); 
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);
            ctx.skillScaling = 1f;
            ctx.isCritical = true;
            ctx.critMultiplier = 1.5f;

            // Fired ONCE per skill execution (before the target loop)
            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            Assert.AreEqual(2.0f, ctx.critMultiplier, "Crit multiplier was not increased correctly.");

            var config = ScriptableObject.CreateInstance<CombatConfig>();
            config.attackRollMin = 1f; 
            config.attackRollMax = 1f;

            // Simulate the per-target loop logic from BattleSystem / DamageEffect
            foreach (var target in targets)
            {
                ctx.primaryTarget = target;
                
                // Base attack is 10. (10 * 1.0 (skill) * 1.0 (dmg mult) * 2.0 (crit mult)) = 20
                int damage = CombatCalculator.CalculateDamage(ctx, config);
                Assert.AreEqual(20, damage, $"AOE Damage calculation failed for {target.gameObject.name}. (Check for modifier snowballing)");
            }

            Object.DestroyImmediate(enemyGo1);
            Object.DestroyImmediate(enemyGo2);
        }

        [Test]
        public void DamageOutputBonusTrinket_IncreasesDamageMultiplierForDamageSkills()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new DamageOutputBonusTrinketStrategy { damageBonusPercent = 25 }; // +25%
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { damagePercent = 1f }; // Damage skill

            var targets = new List<CombatCharacter> { _character }; // Target doesn't matter for this test
            var rng = new System.Random();
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, rng);

            // Trigger global before damage calculation event
            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            Assert.AreEqual(1.25f, ctx.damageMultiplier);
        }

        [Test]
        public void DamageReceivedBonusTrinket_IncreasesDamageMultiplierForIncomingAttacks()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new DamageReceivedBonusTrinketStrategy { damageBonusPercent = 30 }; // +30%
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1); // Character is the wearer
            _character.ActivateTraits(_battleSystem);

            var attackerGo = new GameObject("Attacker");
            var attacker = attackerGo.AddComponent<CombatCharacter>();

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { damagePercent = 1f }; // Damage skill

            var targets = new List<CombatCharacter> { _character }; 
            var rng = new System.Random();
            var ctx = new SkillContext(attacker, skill, targets, _battleSystem, rng);
            ctx.primaryTarget = _character;

            // Trigger per-target event
            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculationPerTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext, CombatCharacter>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx, _character);

            Assert.AreEqual(1.3f, ctx.damageMultiplier);

            Object.DestroyImmediate(attackerGo);
        }

        [Test]
        public void StatusBurstTrinket_CompressesDurationAndMultipliesAmplitude()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new StatusBurstTrinketStrategy { statusTypes = new List<StatusType> { StatusType.Bleed } };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<CombatCharacter>();
            var enemyData = ScriptableObject.CreateInstance<CharacterData>();
            var enemyStats = ScriptableObject.CreateInstance<StatBlockData>();
            enemyData.statPerLevel.Add(enemyStats);
            enemy.characterData = enemyData;
            enemy.InitializeForCombat(Team.Enemy, 1);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var statusEffect = new StatusEffect
            {
                statusType = StatusType.Bleed,
                amplitude = 3,
                duration = 3,
                applicationChance = 200f // ensure it hits
            };
            skill.effects.Add(statusEffect);

            var targets = new List<CombatCharacter> { enemy };
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, new System.Random(42));
            ctx.didHit = true; // Bypass hit check for status execution

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            statusEffect.Execute(ctx, enemy);

            var appliedBleed = enemy.statusEffects.Find(s => s.type == StatusType.Bleed);
            Assert.IsNotNull(appliedBleed, "Bleed status was not applied.");
            Assert.AreEqual(1, appliedBleed.remainingDuration, "Duration was not compressed to 1.");
            Assert.AreEqual(9, appliedBleed.amplitude, "Amplitude was not multiplied by original duration.");

            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void StatusUnresistableTrinket_BypassesResistanceForWearer()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new StatusUnresistableTrinketStrategy { statusTypes = new List<StatusType> { StatusType.Blight } };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            
            // Give the wearer huge blight resist
            var wearerStats = ScriptableObject.CreateInstance<StatBlockData>();
            wearerStats.blightResist = 500;
            _character.characterData.statPerLevel[0] = wearerStats;
            _character.ActivateTraits(_battleSystem);

            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<CombatCharacter>();
            var enemyData = ScriptableObject.CreateInstance<CharacterData>();
            var enemyStatsMock = ScriptableObject.CreateInstance<StatBlockData>();
            enemyData.statPerLevel.Add(enemyStatsMock);
            enemy.characterData = enemyData;
            enemy.InitializeForCombat(Team.Enemy, 1);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            var statusEffect = new StatusEffect
            {
                statusType = StatusType.Blight,
                amplitude = 5,
                duration = 3,
                applicationChance = 100f // Normally 100 - 500 = 0 chance
            };
            skill.effects.Add(statusEffect);

            var targets = new List<CombatCharacter> { _character };
            var ctx = new SkillContext(enemy, skill, targets, _battleSystem, new System.Random(42));
            ctx.didHit = true;
            ctx.primaryTarget = _character;

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculationPerTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext, CombatCharacter>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx, _character);

            statusEffect.Execute(ctx, _character);

            var appliedBlight = _character.statusEffects.Find(s => s.type == StatusType.Blight);
            Assert.IsNotNull(appliedBlight, "Blight was resisted despite the unresistable trinket on the wearer.");

            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void SingleTargetCritAoeHitTrinket_ModifiesCritAndHitProperly()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new SingleTargetCritAoeHitTrinketStrategy();
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            // Create enemy team of 4
            var enemyTeam = new List<CombatCharacter>();
            for (int i = 0; i < 4; i++)
            {
                var eGo = new GameObject($"Enemy{i}");
                var e = eGo.AddComponent<CombatCharacter>();
                var eData = ScriptableObject.CreateInstance<CharacterData>();
                var eStats = ScriptableObject.CreateInstance<StatBlockData>();
                eData.statPerLevel.Add(eStats);
                e.characterData = eData;
                e.InitializeForCombat(Team.Enemy, i + 1);
                enemyTeam.Add(e);
            }

            _battleSystem.StartBattle(new List<CombatCharacter> { _character }, enemyTeam);

            // Test 1: AOE expands targeting to all enemies
            var aoeSkill = ScriptableObject.CreateInstance<SkillData>();
            aoeSkill.maxTargets = 2; // Skill inherently hits 2
            aoeSkill.targetScope = TargetScope.Enemies;
            aoeSkill.modifier = new SkillModifier { damagePercent = 1f };

            // User initially only targets 2 enemies
            var targets = new List<CombatCharacter> { enemyTeam[0], enemyTeam[1] };
            var ctx = new SkillContext(_character, aoeSkill, targets, _battleSystem, new System.Random(42));
            
            var evGlobal = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var delGlobal = (System.Action<SkillContext>)evGlobal.GetValue(_battleSystem);
            delGlobal?.Invoke(ctx);

            Assert.AreEqual(4, ctx.targets.Count, "AOE attack did not expand its targets to hit all enemies.");

            // Test 2: Single Target Guaranteed Crit
            var stSkill = ScriptableObject.CreateInstance<SkillData>();
            stSkill.maxTargets = 1; // Single Target
            stSkill.modifier = new SkillModifier { damagePercent = 1f };

            var ctxST = new SkillContext(_character, stSkill, targets, _battleSystem, new System.Random(42));
            ctxST.isCritical = false; // Initially false

            var evPerTarget = _battleSystem.GetType().GetField("OnBeforeDamageCalculationPerTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var delPerTarget = (System.Action<SkillContext, CombatCharacter>)evPerTarget.GetValue(_battleSystem);
            delPerTarget?.Invoke(ctxST, enemyTeam[0]);

            Assert.IsTrue(ctxST.isCritical, "Single Target attack did not receive guaranteed crit.");

            foreach (var e in enemyTeam)
            {
                Object.DestroyImmediate(e.gameObject);
            }
        }

        [Test]
        public void SelfDamageOnAttackTrinket_DamagesWearerBasedOnMaxHP()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new SelfDamageOnAttackTrinketStrategy { maxHpPercentageDamage = 10f };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            
            var playerStats = ScriptableObject.CreateInstance<StatBlockData>();
            playerStats.maxHP = 100;
            _character.characterData.statPerLevel[0] = playerStats;
            _character.InitializeForCombat(Team.Player, 1);
            _character.currentHP = 100;
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { damagePercent = 1f }; // Must be a damage skill

            var targets = new List<CombatCharacter>(); // Empty is fine for OnBeforeDamageCalculation trigger
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, new System.Random(42));

            var evGlobal = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var delGlobal = (System.Action<SkillContext>)evGlobal.GetValue(_battleSystem);
            delGlobal?.Invoke(ctx);

            // 10% of 100 max HP = 10 damage.
            Assert.AreEqual(90, _character.currentHP, "Wearer did not take the correct self-damage.");
        }

        [Test]
        public void StatModifierTrinket_Unequip_RemovesStatsCorrectly()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new StatModifierTrinketStrategy
            {
                statTarget = StatTarget.Attack,
                amplitudeType = AmplitudeType.Flat,
                amount = 10f
            };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);

            var statsWithTrinket = _character.GetEffectiveStats();
            Assert.AreEqual(20, statsWithTrinket.attack); // Base 10 + 10

            _partyMember.TryUnequipTrinket(trinket);
            _character.InitializeForCombat(Team.Player, 1); // Re-sync active traits
            
            var statsWithoutTrinket = _character.GetEffectiveStats();
            Assert.AreEqual(10, statsWithoutTrinket.attack); // Back to base 10
        }

        [Test]
        public void SingleTargetCritAoeHitTrinket_IgnoresAoeHeals()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new SingleTargetCritAoeHitTrinketStrategy();
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            // Create enemy team to make sure they aren't targeted
            var enemyGo = new GameObject("Enemy1");
            var enemy = enemyGo.AddComponent<CombatCharacter>();
            var enemyData = ScriptableObject.CreateInstance<CharacterData>();
            var enemyStatsMock = ScriptableObject.CreateInstance<StatBlockData>();
            enemyData.statPerLevel.Add(enemyStatsMock);
            enemy.characterData = enemyData;
            enemy.InitializeForCombat(Team.Enemy, 1);
            _battleSystem.StartBattle(new List<CombatCharacter> { _character }, new List<CombatCharacter> { enemy });

            // AOE Heal Skill
            var aoeHealSkill = ScriptableObject.CreateInstance<SkillData>();
            aoeHealSkill.maxTargets = 4;
            aoeHealSkill.targetScope = TargetScope.Allies;
            aoeHealSkill.modifier = new SkillModifier { healPercent = 1f };

            var targets = new List<CombatCharacter> { _character };
            var ctx = new SkillContext(_character, aoeHealSkill, targets, _battleSystem, new System.Random(42));
            
            var evGlobal = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var delGlobal = (System.Action<SkillContext>)evGlobal.GetValue(_battleSystem);
            delGlobal?.Invoke(ctx);

            Assert.AreEqual(1, ctx.targets.Count, "AOE Heal should not expand targets to enemies.");
            Assert.AreSame(_character, ctx.targets[0], "AOE Heal should keep original ally targets.");

            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void SelfDamageOnAttackTrinket_IgnoresHealSkills()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new SelfDamageOnAttackTrinketStrategy { maxHpPercentageDamage = 10f };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            
            var playerStats = ScriptableObject.CreateInstance<StatBlockData>();
            playerStats.maxHP = 100;
            _character.characterData.statPerLevel[0] = playerStats;
            _character.InitializeForCombat(Team.Player, 1);
            _character.currentHP = 100;
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.modifier = new SkillModifier { healPercent = 1f }; // Heal skill, not damage

            var targets = new List<CombatCharacter>(); 
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, new System.Random(42));

            var evGlobal = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var delGlobal = (System.Action<SkillContext>)evGlobal.GetValue(_battleSystem);
            delGlobal?.Invoke(ctx);

            Assert.AreEqual(100, _character.currentHP, "Wearer should not take self-damage on non-damage skills.");
        }

        [Test]
        public void StatusApplicationBonusTrinket_IgnoresAllyBuffs()
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            var strategy = new StatusApplicationBonusTrinketStrategy 
            { 
                statusType = StatusType.Buff, 
                applicationChanceBonus = 50f,
                onlyAgainstEnemies = true
            };
            trinket.effectStrategies.Add(strategy);

            _partyMember.TryEquipTrinket(trinket);
            _character.partyInfo = _partyMember;
            _character.InitializeForCombat(Team.Player, 1);
            _character.ActivateTraits(_battleSystem);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Allies; // Ally targeting

            var targets = new List<CombatCharacter> { _character };
            var ctx = new SkillContext(_character, skill, targets, _battleSystem, new System.Random(42));

            var ev = _battleSystem.GetType().GetField("OnBeforeDamageCalculation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var del = (System.Action<SkillContext>)ev.GetValue(_battleSystem);
            del?.Invoke(ctx);

            string key = "StatusChanceBonus_Buff";
            Assert.IsFalse(ctx.extra.ContainsKey(key), "Status chance bonus should not apply to ally-targeted skills when onlyAgainstEnemies is true.");
        }
    }
}
