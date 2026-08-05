using System.Collections.Generic;
using Nevergreen.Combat;
using Nevergreen.Data;
using NUnit.Framework;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TurnOrderBuilderTests
    {
        private class DummyRandom : System.Random
        {
            public override int Next(int minValue, int maxValue) => minValue;
        }

        [Test]
        public void Build_SortsBySpeedThenEnemyThenFrontRank()
        {
            var p1Data = UnityEngine.ScriptableObject.CreateInstance<CharacterData>();
            p1Data.actionsPerRound = 1;
            var p1 = new UnityEngine.GameObject("p1").AddComponent<CombatCharacter>();
            p1.characterData = p1Data;
            p1.team = Team.Player;
            p1.rank = 2;
            p1.baseStats = new CombatStats { speed = 5, maxHP = 10 };
            p1.currentHP = 10;
            p1.state = LifeState.Alive;

            var p2Data = UnityEngine.ScriptableObject.CreateInstance<CharacterData>();
            p2Data.actionsPerRound = 1;
            var p2 = new UnityEngine.GameObject("p2").AddComponent<CombatCharacter>();
            p2.characterData = p2Data;
            p2.team = Team.Player;
            p2.rank = 1;
            p2.baseStats = new CombatStats { speed = 5, maxHP = 10 };
            p2.currentHP = 10;
            p2.state = LifeState.Alive;

            var e1Data = UnityEngine.ScriptableObject.CreateInstance<CharacterData>();
            e1Data.actionsPerRound = 1;
            var e1 = new UnityEngine.GameObject("e1").AddComponent<CombatCharacter>();
            e1.characterData = e1Data;
            e1.team = Team.Enemy;
            e1.rank = 1;
            e1.baseStats = new CombatStats { speed = 5, maxHP = 10 };
            e1.currentHP = 10;
            e1.state = LifeState.Alive;

            var playerTeam = new List<CombatCharacter> { p1, p2 };
            var enemyTeam = new List<CombatCharacter> { e1 };

            var config = UnityEngine.ScriptableObject.CreateInstance<CombatConfig>();
            config.speedRollMin = 1;
            config.speedRollMax = 1;

            var rng = new DummyRandom();

            var turnOrder = TurnOrderBuilder.Build(playerTeam, enemyTeam, config, rng);

            // All have speed 5, roll is 1, so final speed is 6.
            // Tie-break 1: Enemies go first. -> e1
            // Tie-break 2: Front rank goes first. -> p2 (rank 1), then p1 (rank 2)
            Assert.AreEqual(3, turnOrder.Count);
            Assert.AreEqual(e1, turnOrder[0].character);
            Assert.AreEqual(p2, turnOrder[1].character);
            Assert.AreEqual(p1, turnOrder[2].character);

            UnityEngine.Object.DestroyImmediate(p1.gameObject);
            UnityEngine.Object.DestroyImmediate(p2.gameObject);
            UnityEngine.Object.DestroyImmediate(e1.gameObject);
            UnityEngine.Object.DestroyImmediate(p1Data);
            UnityEngine.Object.DestroyImmediate(p2Data);
            UnityEngine.Object.DestroyImmediate(e1Data);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
