using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class StealthTests
    {
        private CombatConfig config;
        private CombatCharacter player;
        private CombatCharacter enemy;
        private CombatCharacter secondaryEnemy;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();
            player = CombatTestHelper.CreateCombatCharacter("Player", Team.Player, 1, maxHP: 100);
            enemy = CombatTestHelper.CreateCombatCharacter("Enemy", Team.Enemy, 1, maxHP: 100);
            secondaryEnemy = CombatTestHelper.CreateCombatCharacter("SecondaryEnemy", Team.Enemy, 2, maxHP: 100);
        }

        [TearDown]
        public void Teardown()
        {
            if (player != null) Object.DestroyImmediate(player.gameObject);
            if (enemy != null) Object.DestroyImmediate(enemy.gameObject);
            if (secondaryEnemy != null) Object.DestroyImmediate(secondaryEnemy.gameObject);
            ScriptableObject.DestroyImmediate(config);
        }

        private BattleSystem MakeBattleSystem()
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.combatConfig = config;
            
            // Start battle to set up teams
            bs.StartBattle(new List<CombatCharacter> { player }, new List<CombatCharacter> { enemy, secondaryEnemy });

            // Inject deterministic RNG
            var rng = new System.Random(42);
            typeof(BattleSystem).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(bs, rng);

            return bs;
        }

        private void CallExecuteSkill(BattleSystem bs, CombatCharacter user, SkillData skill, List<CombatCharacter> targets)
        {
            var method = typeof(BattleSystem).GetMethod("ExecuteSkill", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { user, skill, targets });
        }

        [Test]
        public void Stealth_PreventsDirectTargeting()
        {
            var bs = MakeBattleSystem();

            // Set up stealth on enemy
            enemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed.");

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.ignoresStealth = false;

            var validTargets = bs.GetValidTargets(player, skill);

            Assert.IsFalse(validTargets.Contains(enemy), "Stealthed enemy should not be in valid targets.");
            Assert.IsTrue(validTargets.Contains(secondaryEnemy), "Non-stealthed enemy should be in valid targets.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Stealth_BypassWithIgnoresStealth()
        {
            var bs = MakeBattleSystem();

            // Set up stealth on enemy
            enemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed.");

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.ignoresStealth = true;

            var validTargets = bs.GetValidTargets(player, skill);

            Assert.IsTrue(validTargets.Contains(enemy), "Stealthed enemy should be in valid targets when skill ignores stealth.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Stealth_AllowsAoeDamage_AndDoesNotBreakStealth()
        {
            var bs = MakeBattleSystem();

            // Set up stealth on secondaryEnemy (rank 2)
            secondaryEnemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(secondaryEnemy.IsStealthed, "Secondary enemy should be stealthed.");

            // Create AOE skill that targets up to 2 enemies
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.maxTargets = 2;
            skill.effects.Add(new DamageEffect());

            // Get valid targets for direct target selection (must be non-stealthed enemy at rank 1)
            var validDirectTargets = bs.GetValidTargets(player, skill);
            Assert.IsTrue(validDirectTargets.Contains(enemy), "Primary target should be targetable.");
            Assert.IsFalse(validDirectTargets.Contains(secondaryEnemy), "Secondary target should not be targetable directly.");

            // Get AOE targets when targeting enemy (rank 1)
            var targets = bs.GetAOETargets(skill, enemy);
            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(enemy, targets[0]);
            Assert.AreEqual(secondaryEnemy, targets[1]);

            // Execute damage
            int initialHP = secondaryEnemy.currentHP;
            CallExecuteSkill(bs, player, skill, targets);

            Assert.Less(secondaryEnemy.currentHP, initialHP, "Stealthed secondary enemy should take AOE damage.");
            Assert.IsTrue(secondaryEnemy.IsStealthed, "Stealthed secondary enemy should RETAIN stealth after taking damage.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Stealth_DoesNotBreakOnOffensiveAction()
        {
            var bs = MakeBattleSystem();

            // Apply stealth to player
            player.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(player.IsStealthed, "Player should be stealthed initially.");

            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;

            // Execute offensive action
            CallExecuteSkill(bs, player, skill, new List<CombatCharacter> { enemy });

            Assert.IsTrue(player.IsStealthed, "Player stealth should NOT break upon executing offensive action.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Stealth_DoesNotBreakOnTakingDamage()
        {
            // Apply stealth to enemy
            enemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed initially.");

            // Apply damage directly
            enemy.TakeDamage(10);

            Assert.IsTrue(enemy.IsStealthed, "Enemy stealth should NOT break upon taking damage.");
        }

        [Test]
        public void Stealth_GuardRedirectionDoesNotBreakStealth()
        {
            var bs = MakeBattleSystem();

            // Guardian is stealthed
            var guardian = CombatTestHelper.CreateCombatCharacter("Guardian", Team.Player, 2, maxHP: 100);
            guardian.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(guardian.IsStealthed, "Guardian should be stealthed.");

            // Player is guarded by Guardian
            player.AddStatus(new GuardStatusInstance(guardian, 3));

            // Setup a damage skill
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.effects.Add(new DamageEffect());

            // Enemy executes skill on player
            int initialGuardianHP = guardian.currentHP;
            int initialPlayerHP = player.currentHP;

            CallExecuteSkill(bs, enemy, skill, new List<CombatCharacter> { player });

            Assert.AreEqual(initialPlayerHP, player.currentHP, "Player should not take damage (guarded).");
            Assert.Less(guardian.currentHP, initialGuardianHP, "Guardian should take damage via redirection.");
            Assert.IsTrue(guardian.IsStealthed, "Guardian stealth should NOT break from taking redirection damage.");

            Object.DestroyImmediate(guardian.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Stealth_ExpiresWhenDurationReachesZero()
        {
            // Apply 1-turn stealth to enemy
            enemy.AddStatus(new StealthStatusInstance(1));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed.");

            // Tick durations down
            StatusProcessor.TickDurations(enemy, config.stunRecoveryResistBonus);

            Assert.IsFalse(enemy.IsStealthed, "Enemy stealth should expire when the duration ticks down to 0.");
        }

        [Test]
        public void RemoveStealthEffect_RemovesStealthOnHit()
        {
            var bs = MakeBattleSystem();

            // Apply stealth to enemy
            enemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed.");

            // Create skill with RemoveStealthEffect
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.ignoresStealth = true; // allow direct targeting for test execution
            skill.effects.Add(new RemoveStealthEffect());

            // Execute skill
            CallExecuteSkill(bs, player, skill, new List<CombatCharacter> { enemy });

            // Assert enemy stealth was removed
            Assert.IsFalse(enemy.IsStealthed, "Enemy should no longer be stealthed after being hit by a skill with RemoveStealthEffect.");

            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void RemoveStealthEffect_DoesNotRemoveStealthOnMiss()
        {
            var bs = MakeBattleSystem();

            // Apply stealth to enemy
            enemy.AddStatus(new StealthStatusInstance(3));
            Assert.IsTrue(enemy.IsStealthed, "Enemy should be stealthed.");

            // Create skill with RemoveStealthEffect that does not ignore miss
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.targetScope = TargetScope.Enemies;
            skill.ignoresStealth = true;
            
            // Set stats to guarantee a miss
            skill.modifier.accuracyMod = -1000f;
            skill.guaranteedHit = false;

            var removeStealthEffect = new RemoveStealthEffect();
            removeStealthEffect.ignoreMiss = false;
            skill.effects.Add(removeStealthEffect);

            // Execute skill
            CallExecuteSkill(bs, player, skill, new List<CombatCharacter> { enemy });

            // Assert enemy stealth was NOT removed because it missed
            Assert.IsTrue(enemy.IsStealthed, "Enemy should remain stealthed when the skill misses and ignoreMiss is false.");

            // Now test with ignoreMiss = true
            removeStealthEffect.ignoreMiss = true;
            CallExecuteSkill(bs, player, skill, new List<CombatCharacter> { enemy });
            Assert.IsFalse(enemy.IsStealthed, "Enemy stealth should be removed when ignoreMiss is true, even on a miss.");

            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
