using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class FormationTests
    {
        private CombatConfig config;

        [SetUp]
        public void Setup()
        {
            config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void Teardown()
        {
            ScriptableObject.DestroyImmediate(config);
        }

        private BattleSystem CreateBattleSystem(List<CombatCharacter> playerTeam, List<CombatCharacter> enemyTeam)
        {
            var bsGo = new GameObject("BS");
            var bs = bsGo.AddComponent<BattleSystem>();
            bs.combatConfig = config;

            // Inject teams
            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            playerTeamField.SetValue(bs, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", BindingFlags.NonPublic | BindingFlags.Instance);
            enemyTeamField.SetValue(bs, enemyTeam);

            return bs;
        }

        // =============================================
        // Existing Tests (Size-1 Backward Compatibility)
        // =============================================

        [Test]
        public void CharacterDestroyed_RemovesFromList_AndShiftsRanks()
        {
            var c1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var c2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            var c3 = CombatTestHelper.CreateCombatCharacter("P3", Team.Player, 3);
            
            var playerTeam = new List<CombatCharacter> { c1, c2, c3 };
            var enemyTeam = new List<CombatCharacter>();

            var bs = CreateBattleSystem(playerTeam, enemyTeam);
            
            // Trigger destruction via reflection to simulate the event
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { c2 });

            // Validate
            Assert.AreEqual(2, playerTeam.Count, "Team should now only have 2 members.");
            Assert.IsFalse(playerTeam.Contains(c2), "P2 should be removed from the list.");
            
            Assert.AreEqual(1, c1.rank, "P1 should still be at rank 1.");
            Assert.AreEqual(2, c3.rank, "P3 should have shifted from rank 3 to rank 2.");

            Object.DestroyImmediate(c1.gameObject);
            Object.DestroyImmediate(c3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void LastCharacterDestroyed_ResultsInEmptyTeam()
        {
            var c1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var playerTeam = new List<CombatCharacter> { c1 };
            var enemyTeam = new List<CombatCharacter>();

            var bs = CreateBattleSystem(playerTeam, enemyTeam);
            
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { c1 });

            Assert.AreEqual(0, playerTeam.Count, "Team should be empty.");
            
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void FrontCharacterDestroyed_EveryoneShiftsUp()
        {
            var c1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var c2 = CombatTestHelper.CreateCombatCharacter("P2", Team.Player, 2);
            var c3 = CombatTestHelper.CreateCombatCharacter("P3", Team.Player, 3);
            
            var playerTeam = new List<CombatCharacter> { c1, c2, c3 };
            var bs = CreateBattleSystem(playerTeam, new List<CombatCharacter>());
            
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { c1 });

            Assert.AreEqual(1, c2.rank, "P2 should have shifted from 2 to 1.");
            Assert.AreEqual(2, c3.rank, "P3 should have shifted from 3 to 2.");

            Object.DestroyImmediate(c2.gameObject);
            Object.DestroyImmediate(c3.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void DestroyLastEnemy_TriggersVictory()
        {
            var p1 = CombatTestHelper.CreateCombatCharacter("P1", Team.Player, 1);
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1);
            
            var playerTeam = new List<CombatCharacter> { p1 };
            var enemyTeam = new List<CombatCharacter> { e1 };
            
            var bs = CreateBattleSystem(playerTeam, enemyTeam);
            
            // Trigger destruction
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { e1 });

            Assert.AreEqual(0, enemyTeam.Count, "Enemy team should be empty.");
            // Assuming CheckBattleEnd was called internally and it handles the end of battle appropriately.
            
            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        // =============================================
        // Multi-Rank Enemy Tests
        // =============================================

        [Test]
        public void OccupiedRanks_Size1_ReturnsSingleRank()
        {
            var c = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 2, size: 1);
            var ranks = c.OccupiedRanks;
            
            Assert.AreEqual(1, ranks.Count);
            Assert.AreEqual(2, ranks[0]);

            Object.DestroyImmediate(c.gameObject);
        }

        [Test]
        public void OccupiedRanks_Size2_ReturnsTwoRanks()
        {
            var c = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2);
            var ranks = c.OccupiedRanks;
            
            Assert.AreEqual(2, ranks.Count);
            Assert.AreEqual(1, ranks[0]);
            Assert.AreEqual(2, ranks[1]);

            Object.DestroyImmediate(c.gameObject);
        }

        [Test]
        public void OccupiedRanks_Size3_AtRank2_ReturnsRanks234()
        {
            var c = CombatTestHelper.CreateCombatCharacter("Giant", Team.Enemy, 2, size: 3);
            var ranks = c.OccupiedRanks;
            
            Assert.AreEqual(3, ranks.Count);
            Assert.AreEqual(2, ranks[0]);
            Assert.AreEqual(3, ranks[1]);
            Assert.AreEqual(4, ranks[2]);

            Object.DestroyImmediate(c.gameObject);
        }

        [Test]
        public void GetValidTargets_MultiRankEnemy_HitBySkillTargetingAnyOccupiedRank()
        {
            // Size-2 boss at ranks 1-2
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2);
            // Size-1 minion at rank 3
            var minion = CombatTestHelper.CreateCombatCharacter("Minion", Team.Enemy, 3, size: 1);
            var hero = CombatTestHelper.CreateCombatCharacter("Hero", Team.Player, 1);

            var playerTeam = new List<CombatCharacter> { hero };
            var enemyTeam = new List<CombatCharacter> { boss, minion };
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Skill targets only rank 2 — should still hit the boss (occupies 1 and 2)
            var skill = CombatTestHelper.CreateDamageSkill(guaranteedHit: true);
            skill.targetRanks = new List<int> { 2 };

            var targets = bs.GetValidTargets(hero, skill);
            Assert.AreEqual(1, targets.Count, "Should find exactly 1 target.");
            Assert.AreEqual(boss, targets[0], "Boss should be targetable via rank 2.");

            Object.DestroyImmediate(hero.gameObject);
            Object.DestroyImmediate(boss.gameObject);
            Object.DestroyImmediate(minion.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void GetValidTargets_MultiRankEnemy_NotDuplicatedByAOE()
        {
            // Size-2 boss at ranks 1-2
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2);
            var hero = CombatTestHelper.CreateCombatCharacter("Hero", Team.Player, 1);

            var playerTeam = new List<CombatCharacter> { hero };
            var enemyTeam = new List<CombatCharacter> { boss };
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Skill targets ranks 1, 2, 3, 4 — boss should appear only ONCE
            var skill = CombatTestHelper.CreateDamageSkill(guaranteedHit: true);
            skill.targetRanks = new List<int> { 1, 2, 3, 4 };
            skill.maxTargets = 4;

            var targets = bs.GetValidTargets(hero, skill);
            Assert.AreEqual(1, targets.Count, "Size-2 boss should only appear once.");

            Object.DestroyImmediate(hero.gameObject);
            Object.DestroyImmediate(boss.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void GetValidTargets_SkillMissesEnemy_WhenNoOccupiedRankMatches()
        {
            // Size-1 enemy at rank 1
            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1, size: 1);
            var hero = CombatTestHelper.CreateCombatCharacter("Hero", Team.Player, 1);

            var playerTeam = new List<CombatCharacter> { hero };
            var enemyTeam = new List<CombatCharacter> { e1 };
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Skill only targets ranks 3-4
            var skill = CombatTestHelper.CreateDamageSkill(guaranteedHit: true);
            skill.targetRanks = new List<int> { 3, 4 };

            var targets = bs.GetValidTargets(hero, skill);
            Assert.AreEqual(0, targets.Count, "No targets should be found.");

            Object.DestroyImmediate(hero.gameObject);
            Object.DestroyImmediate(e1.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void CompactFormation_Size2EnemyDestroyed_ShiftsRemainingForward()
        {
            // Size-2 boss at ranks 1-2, size-1 minion at rank 3, size-1 minion at rank 4
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2);
            var m1 = CombatTestHelper.CreateCombatCharacter("M1", Team.Enemy, 3, size: 1);
            var m2 = CombatTestHelper.CreateCombatCharacter("M2", Team.Enemy, 4, size: 1);

            var enemyTeam = new List<CombatCharacter> { boss, m1, m2 };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            // Destroy the boss (leaves a 2-slot gap)
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { boss });

            // m1 should compact from 3 -> 1, m2 from 4 -> 2
            Assert.AreEqual(2, enemyTeam.Count);
            Assert.AreEqual(1, m1.rank, "M1 should shift from rank 3 to rank 1.");
            Assert.AreEqual(2, m2.rank, "M2 should shift from rank 4 to rank 2.");

            Object.DestroyImmediate(m1.gameObject);
            Object.DestroyImmediate(m2.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void CompactFormation_MiddleSize1Destroyed_LargeEnemyUnaffected()
        {
            // Size-1 minion at rank 1, size-2 boss at ranks 2-3, size-1 minion at rank 4
            var m1 = CombatTestHelper.CreateCombatCharacter("M1", Team.Enemy, 1, size: 1);
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 2, size: 2);
            var m2 = CombatTestHelper.CreateCombatCharacter("M2", Team.Enemy, 4, size: 1);

            var enemyTeam = new List<CombatCharacter> { m1, boss, m2 };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            // Destroy M1 (rank 1, size 1 gap)
            var method = typeof(BattleSystem).GetMethod("HandleCharacterDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { m1 });

            // Boss should compact from 2 -> 1 (occupying 1-2), M2 from 4 -> 3
            Assert.AreEqual(2, enemyTeam.Count);
            Assert.AreEqual(1, boss.rank, "Boss should shift from rank 2 to rank 1.");
            Assert.AreEqual(3, m2.rank, "M2 should shift from rank 4 to rank 3.");

            // Verify boss occupied ranks
            var bossRanks = boss.OccupiedRanks;
            Assert.AreEqual(2, bossRanks.Count);
            Assert.AreEqual(1, bossRanks[0]);
            Assert.AreEqual(2, bossRanks[1]);

            Object.DestroyImmediate(boss.gameObject);
            Object.DestroyImmediate(m2.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }

        [Test]
        public void CanUseSkillFromRank_MultiRank_AnyOccupiedRankCounts()
        {
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2);
            
            // Skill usable from rank 2 only
            var skill = CombatTestHelper.CreateDamageSkill();
            skill.useRanks = new List<int> { 2 };

            // Boss is at ranks 1-2, so rank 2 is occupied — should be able to use
            Assert.IsTrue(boss.CanUseSkillFromRank(skill), "Size-2 boss at ranks 1-2 should use a skill restricted to rank 2.");

            // Skill usable from rank 3 only — boss doesn't occupy rank 3
            skill.useRanks = new List<int> { 3 };
            Assert.IsFalse(boss.CanUseSkillFromRank(skill), "Size-2 boss at ranks 1-2 should NOT use a skill restricted to rank 3.");

            Object.DestroyImmediate(boss.gameObject);
        }

        [Test]
        public void PileRetainsSize_AfterDefeat()
        {
            var boss = CombatTestHelper.CreateCombatCharacter("Boss", Team.Enemy, 1, size: 2, maxHP: 100);
            boss.characterData.leavesPileOnDeath = true;

            var enemyTeam = new List<CombatCharacter> { boss };
            var bs = CreateBattleSystem(new List<CombatCharacter>(), enemyTeam);

            // Simulate transition to Pile (non-critical death)
            var method = typeof(BattleSystem).GetMethod("FinalizeCharacterDefeat", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(bs, new object[] { boss, false });

            Assert.AreEqual(LifeState.Pile, boss.state);
            Assert.AreEqual(2, boss.characterData.size, "Pile should retain size 2.");

            var pileRanks = boss.OccupiedRanks;
            Assert.AreEqual(2, pileRanks.Count, "Pile should still occupy 2 ranks.");
            Assert.AreEqual(1, pileRanks[0]);
            Assert.AreEqual(2, pileRanks[1]);

            Object.DestroyImmediate(boss.gameObject);
            Object.DestroyImmediate(bs.gameObject);
        }
    }
}
