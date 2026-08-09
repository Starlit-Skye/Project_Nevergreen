using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class CeciliaDefeatTests
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

            var playerTeamField = typeof(BattleSystem).GetField("_playerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerTeamField.SetValue(bs, playerTeam);

            var enemyTeamField = typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemyTeamField.SetValue(bs, enemyTeam);

            return bs;
        }

        [Test]
        public void CeciliaDefeat_ImmediatelyEndsBattle_EvenIfRemoved()
        {
            var ceciliaData = ScriptableObject.CreateInstance<CharacterData>();
            ceciliaData.characterId = "ceci";
            ceciliaData.leavesPileOnDeath = false; // So she gets destroyed immediately

            var p1Go = new GameObject("Cecilia");
            var p1 = p1Go.AddComponent<CombatCharacter>();
            p1.characterData = ceciliaData;
            p1.team = Team.Player;
            p1.baseStats = new CombatStats { maxHP = 10 };
            p1.currentHP = 10;
            p1.state = LifeState.Alive;

            var e1 = CombatTestHelper.CreateCombatCharacter("E1", Team.Enemy, 1);
            
            var playerTeam = new List<CombatCharacter> { p1 };
            var enemyTeam = new List<CombatCharacter> { e1 };
            
            var bs = CreateBattleSystem(playerTeam, enemyTeam);

            // Act: Start battle to hook up events
            bs.StartBattle(playerTeam, enemyTeam);

            // Act: Deal fatal damage
            p1.TakeDamage(20, false);

            // Validate
            // CheckBattleEnd should have been called, setting state to BattleEnd
            var stateProp = typeof(BattleSystem).GetProperty("CurrentState");
            var currentState = (BattleState)stateProp.GetValue(bs);

            Assert.AreEqual(BattleState.BattleEnd, currentState, "Battle should have ended when Cecilia was defeated.");
            Assert.AreEqual(0, playerTeam.Count, "Cecilia should have been removed from the team.");

            if (p1 != null && p1.gameObject != null) Object.DestroyImmediate(p1.gameObject);
            if (e1 != null && e1.gameObject != null) Object.DestroyImmediate(e1.gameObject);
            if (bs != null && bs.gameObject != null) Object.DestroyImmediate(bs.gameObject);
            ScriptableObject.DestroyImmediate(ceciliaData);
        }
    }
}
