using System.Collections.Generic;
using Nevergreen.Combat;
using Nevergreen.Data;
using NUnit.Framework;
using UnityEngine;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TargetResolverTests
    {
        [Test]
        public void GetAOETargets_ExpandsToMaxTargets_SkipsDead()
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.maxTargets = 3;

            var p1 = new GameObject("p1").AddComponent<CombatCharacter>();
            p1.rank = 1;
            p1.state = LifeState.Alive;
            p1.team = Team.Player;

            var p2 = new GameObject("p2").AddComponent<CombatCharacter>();
            p2.rank = 2;
            p2.state = LifeState.Dying; // Dead, should be skipped
            p2.team = Team.Player;

            var p3 = new GameObject("p3").AddComponent<CombatCharacter>();
            p3.rank = 3;
            p3.state = LifeState.Alive;
            p3.team = Team.Player;
            
            var p4 = new GameObject("p4").AddComponent<CombatCharacter>();
            p4.rank = 4;
            p4.state = LifeState.Pile; // Piles count for AOE expansion
            p4.team = Team.Player;

            var p5 = new GameObject("p5").AddComponent<CombatCharacter>();
            p5.rank = 5;
            p5.state = LifeState.Alive;
            p5.team = Team.Player;

            var playerTeam = new List<CombatCharacter> { p1, p2, p3, p4, p5 };
            var enemyTeam = new List<CombatCharacter>();

            // Target p1, maxTargets 3
            var targets = TargetResolver.GetAOETargets(skill, p1, playerTeam, enemyTeam);

            Assert.AreEqual(3, targets.Count);
            Assert.AreEqual(p1, targets[0]);
            Assert.AreEqual(p3, targets[1]); // Skipped p2
            Assert.AreEqual(p4, targets[2]); // Included pile p4

            Object.DestroyImmediate(p1.gameObject);
            Object.DestroyImmediate(p2.gameObject);
            Object.DestroyImmediate(p3.gameObject);
            Object.DestroyImmediate(p4.gameObject);
            Object.DestroyImmediate(p5.gameObject);
            Object.DestroyImmediate(skill);
        }
    }
}
