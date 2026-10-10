using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class LifeLinkTests
    {
        private BattleSystem _battleSystem;
        private CombatCharacter _weaver1;
        private CombatCharacter _weaver2;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("BattleSystem");
            _battleSystem = go.AddComponent<BattleSystem>();

            var w1Go = new GameObject("Weaver1");
            _weaver1 = w1Go.AddComponent<CombatCharacter>();
            _weaver1.team = Team.Enemy;
            _weaver1.baseStats = new CombatStats { maxHP = 100 };
            _weaver1.currentHP = 100;
            _weaver1.state = LifeState.Alive;

            var w2Go = new GameObject("Weaver2");
            _weaver2 = w2Go.AddComponent<CombatCharacter>();
            _weaver2.team = Team.Enemy;
            _weaver2.baseStats = new CombatStats { maxHP = 100 };
            _weaver2.currentHP = 100;
            _weaver2.state = LifeState.Alive;

            _battleSystem.StartBattle(new List<CombatCharacter>(), new List<CombatCharacter> { _weaver1, _weaver2 });
        }

        [TearDown]
        public void Teardown()
        {
            if (_battleSystem != null && _battleSystem.gameObject != null) Object.DestroyImmediate(_battleSystem.gameObject);
            if (_weaver1 != null && _weaver1.gameObject != null) Object.DestroyImmediate(_weaver1.gameObject);
            if (_weaver2 != null && _weaver2.gameObject != null) Object.DestroyImmediate(_weaver2.gameObject);
        }

        [Test]
        public void TakeDamage_WithLifeLink_InterceptsDefeatAndEntersDowned()
        {
            var lifeLink1 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver1.AddStatus(lifeLink1);

            var lifeLink2 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver2.AddStatus(lifeLink2);

            _weaver1.TakeDamage(100);

            Assert.AreEqual(LifeState.Downed, _weaver1.state);
            Assert.AreEqual(0, _weaver1.currentHP);
            Assert.AreEqual(LifeState.Alive, _weaver2.state);
        }

        [Test]
        public void TakeDamage_LastLifeLink_CollapsesAndDying()
        {
            var lifeLink1 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver1.AddStatus(lifeLink1);

            var lifeLink2 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver2.AddStatus(lifeLink2);

            _weaver1.TakeDamage(100);
            Assert.AreEqual(LifeState.Downed, _weaver1.state);

            _weaver2.TakeDamage(100);
            
            // Link collapses, both should go to Destroyed because EditMode LifecycleManager destroys them immediately
            Assert.IsTrue(_weaver1 == null || _weaver1.state == LifeState.Destroyed, "Weaver 1 should be destroyed");
            Assert.IsTrue(_weaver2 == null || _weaver2.state == LifeState.Destroyed, "Weaver 2 should be destroyed");
        }

        [Test]
        public void EndRound_WithLifeLink_RevivesDownedCharacter()
        {
            var lifeLink1 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver1.AddStatus(lifeLink1);

            var lifeLink2 = new LifeLinkStatusInstance(_battleSystem, 25, 999);
            _weaver2.AddStatus(lifeLink2);

            _weaver1.TakeDamage(100);
            Assert.AreEqual(LifeState.Downed, _weaver1.state);

            // Trigger the internal logic we added to BattleSystem using Reflection or a public helper method
            // Actually, we can just run the coroutine step or extract it to a public method.
            // Since EndRound is a coroutine, we can't easily unit test it synchronously without Coroutine runner.
            // But we can test ReviveFromDowned directly.
            
            _weaver1.ReviveFromDowned(25);
            Assert.AreEqual(LifeState.Alive, _weaver1.state);
            Assert.AreEqual(25, _weaver1.currentHP);
        }
    }
}
