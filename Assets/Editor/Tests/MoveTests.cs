using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class MoveTests
    {
        private CombatConfig config;
        private System.Random rng;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            config = CombatTestHelper.CreateDefaultConfig();
            rng = CombatTestHelper.CreateFixedRng(42);
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            ScriptableObject.DestroyImmediate(config);
        }

        private BattleSystem CreateBattleSystem(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();

            // Use reflection to set the private fields without starting the Coroutine
            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            playerTeamField.SetValue(bs, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            enemyTeamField.SetValue(bs, enemyTeam);

            var animQueue = bsGo.AddComponent<AnimationQueueProcessor>();

            return bs;
        }

        [Test]
        public void ShiftRank_MoveStatusInstance_DisplacesOtherCharacters()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            var char3 = CombatTestHelper.CreateCombatCharacter("P3", Team.Player, 3);
            
            var playerTeam = new List<CombatCharacter> { char1, char2, char3 };
            var enemyTeam = new List<CombatCharacter>();

            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Create Move Status (Amplitude 1: means move backward 1 rank for player? Wait, amplitude is added to rank.
            // Rank 1 + 1 = Rank 2)
            var moveStatus = new MoveStatusInstance(bs, 1);
            
            // Add status to P1
            char1.AddStatus(moveStatus);

            // Validate that rank was shifted
            Assert.AreEqual(2, char1.rank, "P1 should have moved to rank 2.");
            Assert.AreEqual(1, char2.rank, "P2 should have been shifted to rank 1 to make room.");
            Assert.AreEqual(3, char3.rank, "P3 should remain at rank 3.");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(char3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void ResistanceCheck_SubtractsMoveResist_AndFailsIfBelowChance()
        {
            // Move Resist 300 (e.g. Pile) vs application chance
            var target = CombatTestHelper.CreateCombatCharacter("Target", Team.Enemy, 1, moveResist: 300);
            var attacker = CombatTestHelper.CreateCombatCharacter("Attacker", Team.Player, 1);
            
            var playerTeam = new List<CombatCharacter> { attacker };
            var enemyTeam = new List<CombatCharacter> { target };
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Create Skill Context
            var skill = CombatTestHelper.CreateDamageSkill();
            var ctx = new SkillContext(attacker, skill, enemyTeam, bs, rng);
            ctx.didHit = true; // Assume the attack hit

            // Execute StatusEffect
            var moveEffect = new Nevergreen.Combat.StatusEffect
            {
                statusType = StatusType.Move,
                applicationChance = 100,
                amplitude = 1,
                duration = 0
            };

            moveEffect.Execute(ctx, target);

            // target should NOT have moved (rank should still be 1)
            Assert.AreEqual(1, target.rank, "Target rank should not change because move was resisted.");
            
            // And it should have 0 move statuses
            Assert.AreEqual(0, target.statusEffects.Count, "No move status should be present.");

            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Clamping_DoesNotExceedMaxRanks()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            
            var playerTeam = new List<CombatCharacter> { char1, char2 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            // Move by 3 (should clamp to team.Count = 2)
            var moveStatus = new MoveStatusInstance(bs, 3);
            char1.AddStatus(moveStatus);

            Assert.AreEqual(2, char1.rank, "P1 rank should be clamped to 2 (team size).");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void InstantExpiration_MoveStatusExpiresImmediately()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            
            var playerTeam = new List<CombatCharacter> { char1 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            var moveStatus = new MoveStatusInstance(bs, 1);
            char1.AddStatus(moveStatus);

            Assert.IsTrue(moveStatus.IsExpired, "MoveStatus should be expired immediately due to 0 duration.");
            Assert.AreEqual(0, char1.statusEffects.Count, "MoveStatus should be removed immediately from the character's status list.");
            
            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }
        [Test]
        public void NegativeAmplitude_MovesForward_DisplacesCorrectly()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            var char3 = CombatTestHelper.CreateCombatCharacter("P3", Team.Player, 3);
            
            var playerTeam = new List<CombatCharacter> { char1, char2, char3 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            // Move P3 forward by 2 ranks (amplitude -2)
            var moveStatus = new MoveStatusInstance(bs, -2);
            char3.AddStatus(moveStatus);

            Assert.AreEqual(1, char3.rank, "P3 should have moved to rank 1.");
            Assert.AreEqual(2, char1.rank, "P1 should have been shifted back to rank 2.");
            Assert.AreEqual(3, char2.rank, "P2 should have been shifted back to rank 3.");

            Object.DestroyImmediate(char1.gameObject);
            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(char3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Clamping_DoesNotExceedMinRanks()
        {
            var char2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            
            var playerTeam = new List<CombatCharacter> { char2 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());

            // Move forward by 5 (should clamp to 1)
            var moveStatus = new MoveStatusInstance(bs, -5);
            char2.AddStatus(moveStatus);

            Assert.AreEqual(1, char2.rank, "P2 rank should be clamped to 1.");

            Object.DestroyImmediate(char2.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void NullBattleSystem_FailsGracefully()
        {
            var char1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            
            // Pass null for BattleSystem
            var moveStatus = new MoveStatusInstance(null, 1);
            
            // Should not throw exception
            Assert.DoesNotThrow(() => char1.AddStatus(moveStatus), "Adding move status with null BattleSystem should not throw.");
            
            // Rank should remain unchanged
            Assert.AreEqual(1, char1.rank, "Rank should not change when BattleSystem is null.");
            
            Object.DestroyImmediate(char1.gameObject);
        }

        [Test]
        public void BugReproduction_SmallTeam_LargeAmplitude()
        {
            // Criteria: 
            // 1. Enemy team only has 3 characters (Ranks 1, 2, 3)
            // 2. Move Status amplitude is 5
            // 3. Move Status is applied to character at rank 1
            
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1);
            var e2 = CombatTestHelper.CreateCombatCharacter("E2", Team.Enemy, 2);
            var e3 = CombatTestHelper.CreateCombatCharacter("E3", Team.Enemy, 3);
            
            var enemyTeam = new List<CombatCharacter> { e1, e2, e3 };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            // Move E1 (Rank 1) by amplitude 5 -> Rank 6 -> Clamped to team.Count = 3
            var moveStatus = new MoveStatusInstance(bs, 5);
            e1.AddStatus(moveStatus);

            // Expectation: E1 moves to 3, E2 moves to 1, E3 moves to 2
            Assert.AreEqual(3, e1.rank, "E1 should have moved to rank 3.");
            Assert.AreEqual(1, e2.rank, "E2 should have moved to rank 1.");
            Assert.AreEqual(2, e3.rank, "E3 should have moved to rank 2.");

            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(e2.gameObject);
            Object.DestroyImmediate(e3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void Pile_InnateMoveResist_Is300()
        {
            var target = CombatTestHelper.CreateCombatCharacter("Pile", Team.Enemy, 1);
            target.state = LifeState.Pile;

            CombatStats stats = target.GetEffectiveStats();
            Assert.IsTrue(stats.moveResist >= 300, $"Pile should have high move resist. Actual: {stats.moveResist}");
        }
    }
}
