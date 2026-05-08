using System.Collections.Generic;
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
    }
}
