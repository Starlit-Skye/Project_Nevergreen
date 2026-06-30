using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Prototype;
using System.Collections.Generic;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class GainPartOnKillEffectTests
    {
        private List<GameObject> _cleanup;
        private InBattleRewardUI _rewardUI;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            CombatTestHelper.InitializeTestDatabase();

            // Create a standalone InBattleRewardUI for testing
            var uiGo = new GameObject("InBattleRewardUI");
            _cleanup.Add(uiGo);
            _rewardUI = uiGo.AddComponent<InBattleRewardUI>();
            _rewardUI.panel = new GameObject("Panel");
            _rewardUI.panel.SetActive(false);
            _cleanup.Add(_rewardUI.panel);

            // Ensure static state is clean
            RunSessionManager.Parts = 0;
        }

        [TearDown]
        public void TearDown()
        {
            CombatTestHelper.CleanupTestDatabase();

            foreach (var go in _cleanup)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Execute_TargetDefeated_GrantsPartAndTriggersPopup()
        {
            var user = CombatTestHelper.CreateCombatCharacter("user", Team.Player, 1);
            _cleanup.Add(user.gameObject);
            var skill = ScriptableObject.CreateInstance<Nevergreen.Data.SkillData>();

            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1, maxHP: 100);
            _cleanup.Add(target.gameObject);

            // Defeat the target
            target.currentHP = 0;
            target.state = LifeState.Dying;

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, new System.Random());
            var effect = new GainPartOnKillEffect();

            int initialParts = RunSessionManager.Parts;

            effect.Execute(ctx, target);

            Assert.AreEqual(initialParts + 1, RunSessionManager.Parts, "Parts should increase by 1 on kill.");

            // InBattleRewardUI panel should be activated
            Assert.IsTrue(_rewardUI.panel.activeSelf, "In-battle reward panel should be visible.");
        }

        [Test]
        public void Execute_TargetAlive_NoPartGranted()
        {
            var user = CombatTestHelper.CreateCombatCharacter("user", Team.Player, 1);
            _cleanup.Add(user.gameObject);
            var skill = ScriptableObject.CreateInstance<Nevergreen.Data.SkillData>();

            var target = CombatTestHelper.CreateCombatCharacter("target", Team.Enemy, 1, maxHP: 100);
            _cleanup.Add(target.gameObject);

            // Target is alive
            target.currentHP = 100;
            target.state = LifeState.Alive;

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, new System.Random());
            var effect = new GainPartOnKillEffect();

            int initialParts = RunSessionManager.Parts;

            effect.Execute(ctx, target);

            Assert.AreEqual(initialParts, RunSessionManager.Parts, "Parts should not increase if target survives.");
            Assert.IsFalse(_rewardUI.panel.activeSelf, "In-battle reward panel should not pop up.");
        }
    }
}
