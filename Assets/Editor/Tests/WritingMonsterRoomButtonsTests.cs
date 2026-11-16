using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nevergreen;
using Nevergreen.Prototype;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class WritingMonsterRoomButtonsTests
    {
        private List<Object> _objectsToCleanUp;
        private int _initialExhaustionRound;
        private int _initialExhaustionLimit;
        private int _initialParts;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
            _initialExhaustionRound = RunSessionManager.ExhaustionRound;
            _initialExhaustionLimit = RunSessionManager.ExhaustionLimit;
            _initialParts = RunSessionManager.Parts;
        }

        [TearDown]
        public void TearDown()
        {
            RunSessionManager.ExhaustionRound = _initialExhaustionRound;
            RunSessionManager.ExhaustionLimit = _initialExhaustionLimit;
            RunSessionManager.Parts = _initialParts;

            if (_objectsToCleanUp != null)
            {
                foreach (var obj in _objectsToCleanUp)
                {
                    if (obj != null && !UnityEditor.EditorUtility.IsPersistent(obj))
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
                _objectsToCleanUp.Clear();
            }
        }

        private WritingMonsterRoomButtons CreateController()
        {
            var go = new GameObject("WritingMonsterButtonsUI");
            _objectsToCleanUp.Add(go);
            return go.AddComponent<WritingMonsterRoomButtons>();
        }

        [Test]
        public void WritingMonsterRoomButtons_Defaults_AreSetToOne()
        {
            var controller = CreateController();

            Assert.AreEqual(1, controller.ExhaustionReduction, "Default exhaustionReduction should be 1.");
            Assert.AreEqual(1, controller.PartsReward, "Default partsReward should be 1.");
            Assert.IsFalse(controller.HasChosen, "HasChosen should initially be false.");
        }

        [Test]
        public void HelpMonster_ReducesExhaustionRound_ByConfiguredAmount()
        {
            RunSessionManager.ExhaustionLimit = 10;
            RunSessionManager.ExhaustionRound = 5;

            var controller = CreateController();
            controller.ExhaustionReduction = 2;

            controller.HelpMonster();

            Assert.AreEqual(3, RunSessionManager.ExhaustionRound, "5 - 2 should equal 3.");
            Assert.IsTrue(controller.HasChosen, "HasChosen should be true after HelpMonster.");
        }

        [Test]
        public void HelpMonster_ClampsAtZero_WhenReductionExceedsCurrent()
        {
            RunSessionManager.ExhaustionLimit = 10;
            RunSessionManager.ExhaustionRound = 1;

            var controller = CreateController();
            controller.ExhaustionReduction = 5;

            controller.HelpMonster();

            Assert.AreEqual(0, RunSessionManager.ExhaustionRound, "Exhaustion round should be clamped at minimum 0.");
        }

        [Test]
        public void HelpMonster_FiresOnExhaustionChanged_OnlyWhenValueChanged()
        {
            RunSessionManager.ExhaustionLimit = 10;
            RunSessionManager.ExhaustionRound = 3;

            int changeCount = 0;
            System.Action onExhaustionChanged = () => changeCount++;
            RunSessionManager.OnExhaustionChanged += onExhaustionChanged;

            try
            {
                var controller1 = CreateController();
                controller1.ExhaustionReduction = 1;
                controller1.HelpMonster();

                Assert.AreEqual(1, changeCount, "OnExhaustionChanged should fire once when round decreases 3 -> 2.");

                // When already 0, reducing further should not fire event
                RunSessionManager.ExhaustionRound = 0;
                changeCount = 0;

                var controller2 = CreateController();
                controller2.ExhaustionReduction = 1;
                controller2.HelpMonster();

                Assert.AreEqual(0, changeCount, "OnExhaustionChanged should not fire when already 0.");
            }
            finally
            {
                RunSessionManager.OnExhaustionChanged -= onExhaustionChanged;
            }
        }

        [Test]
        public void HelpMonster_WorksNormally_WhenExhaustionLimitIsZero()
        {
            RunSessionManager.ExhaustionLimit = 0;
            RunSessionManager.ExhaustionRound = 4;

            var controller = CreateController();
            controller.ExhaustionReduction = 2;

            controller.HelpMonster();

            Assert.AreEqual(2, RunSessionManager.ExhaustionRound, "Should decrement normally even if limit <= 0.");
            Assert.AreEqual(0, RunSessionManager.ExhaustionLimit, "ExhaustionLimit should remain untouched.");
        }

        [Test]
        public void AttackMonster_GrantsParts_AndFiresOnPartsChanged()
        {
            RunSessionManager.Parts = 10;

            int changeCount = 0;
            System.Action onPartsChanged = () => changeCount++;
            RunSessionManager.OnPartsChanged += onPartsChanged;

            try
            {
                var controller = CreateController();
                controller.PartsReward = 5;

                controller.AttackMonster();

                Assert.AreEqual(15, RunSessionManager.Parts, "Parts should increase from 10 to 15.");
                Assert.AreEqual(1, changeCount, "OnPartsChanged should fire once.");
                Assert.IsTrue(controller.HasChosen, "HasChosen should be true after AttackMonster.");
            }
            finally
            {
                RunSessionManager.OnPartsChanged -= onPartsChanged;
            }
        }

        [Test]
        public void AttackMonster_ZeroReward_DoesNotChangeParts()
        {
            RunSessionManager.Parts = 10;

            var controller = CreateController();
            controller.PartsReward = 0;

            controller.AttackMonster();

            Assert.AreEqual(10, RunSessionManager.Parts, "Parts should remain 10 when partsReward is 0.");
        }

        [Test]
        public void OneChoiceRule_HelpThenAttack_BlocksAttack()
        {
            RunSessionManager.ExhaustionLimit = 10;
            RunSessionManager.ExhaustionRound = 5;
            RunSessionManager.Parts = 10;

            var controller = CreateController();
            controller.ExhaustionReduction = 2;
            controller.PartsReward = 5;

            controller.HelpMonster();
            Assert.AreEqual(3, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(controller.HasChosen);

            // Attempt attack
            controller.AttackMonster();
            Assert.AreEqual(10, RunSessionManager.Parts, "Parts should not be granted because choice was already made.");
        }

        [Test]
        public void OneChoiceRule_AttackThenHelp_BlocksHelp()
        {
            RunSessionManager.ExhaustionLimit = 10;
            RunSessionManager.ExhaustionRound = 5;
            RunSessionManager.Parts = 10;

            var controller = CreateController();
            controller.ExhaustionReduction = 2;
            controller.PartsReward = 5;

            controller.AttackMonster();
            Assert.AreEqual(15, RunSessionManager.Parts);
            Assert.IsTrue(controller.HasChosen);

            // Attempt help
            controller.HelpMonster();
            Assert.AreEqual(5, RunSessionManager.ExhaustionRound, "Exhaustion should not reduce because choice was already made.");
        }

        [Test]
        public void OneChoiceRule_RepeatedCalls_OnlyApplyOnce()
        {
            RunSessionManager.Parts = 10;

            var controller = CreateController();
            controller.PartsReward = 5;

            controller.AttackMonster();
            controller.AttackMonster();

            Assert.AreEqual(15, RunSessionManager.Parts, "Second AttackMonster call should be ignored.");
        }

        [Test]
        public void Close_DeactivatesGameObject_AndNotifiesCombatUI()
        {
            var combatUIGo = new GameObject("CombatUI");
            _objectsToCleanUp.Add(combatUIGo);
            combatUIGo.AddComponent<CombatUI>();

            var controller = CreateController();
            Assert.IsTrue(controller.gameObject.activeSelf);

            controller.Close();

            Assert.IsFalse(controller.gameObject.activeSelf, "Close should deactivate the UI GameObject.");
        }

        [Test]
        public void Close_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            var controller = CreateController();

            LogAssert.Expect(LogType.Error, "[WritingMonsterRoomButtons] CombatUI not found!");
            controller.Close();

            Assert.IsFalse(controller.gameObject.activeSelf, "Should still deactivate even if CombatUI is not present.");
        }

        [Test]
        public void Close_WorksRegardlessOfHasChosen()
        {
            var combatUIGo = new GameObject("CombatUI");
            _objectsToCleanUp.Add(combatUIGo);
            combatUIGo.AddComponent<CombatUI>();

            var controller = CreateController();
            controller.HelpMonster();
            Assert.IsTrue(controller.HasChosen);

            controller.Close();
            Assert.IsFalse(controller.gameObject.activeSelf, "Close should succeed after choice.");
        }
    }
}
