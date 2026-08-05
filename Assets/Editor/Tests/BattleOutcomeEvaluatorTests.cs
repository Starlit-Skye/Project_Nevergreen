using System.Collections.Generic;
using Nevergreen.Combat;
using Nevergreen.Data;
using NUnit.Framework;
using UnityEngine;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class BattleOutcomeEvaluatorTests
    {
        [Test]
        public void Evaluate_CeciliaDefeated_ReturnsDefeat()
        {
            var ceciData = ScriptableObject.CreateInstance<CharacterData>();
            ceciData.characterId = "ceci";
            
            var ceci = new GameObject("ceci").AddComponent<CombatCharacter>();
            ceci.characterData = ceciData;
            ceci.state = LifeState.Dying; // Defeated

            var other = new GameObject("other").AddComponent<CombatCharacter>();
            other.state = LifeState.Alive; // Still alive

            var initialTeam = new List<CombatCharacter> { ceci, other };
            var currentTeam = new List<CombatCharacter> { ceci, other };
            var enemies = new List<CombatCharacter> { new GameObject("enemy").AddComponent<CombatCharacter>() };
            enemies[0].state = LifeState.Alive;

            var outcome = BattleOutcomeEvaluator.Evaluate(currentTeam, enemies, initialTeam, out string reason);

            Assert.AreEqual(BattleOutcome.Defeat, outcome);
            Assert.AreEqual("CECILIA DEFEATED", reason);

            Object.DestroyImmediate(ceci.gameObject);
            Object.DestroyImmediate(other.gameObject);
            Object.DestroyImmediate(enemies[0].gameObject);
            Object.DestroyImmediate(ceciData);
        }
        
        [Test]
        public void Evaluate_AllEnemiesDead_ReturnsVictory()
        {
            var p1 = new GameObject("p1").AddComponent<CombatCharacter>();
            p1.state = LifeState.Alive;

            var e1 = new GameObject("e1").AddComponent<CombatCharacter>();
            e1.state = LifeState.Pile; // Piles don't count as alive enemies

            var initialTeam = new List<CombatCharacter> { p1 };
            var currentTeam = new List<CombatCharacter> { p1 };
            var enemies = new List<CombatCharacter> { e1 };

            var outcome = BattleOutcomeEvaluator.Evaluate(currentTeam, enemies, initialTeam, out string reason);

            Assert.AreEqual(BattleOutcome.Victory, outcome);

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(e1.gameObject);
        }
    }
}
