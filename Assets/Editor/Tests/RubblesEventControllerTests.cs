using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Nevergreen.Data;
using Nevergreen.UI;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class RubblesEventControllerTests
    {
        private GameObject _uiRoot;
        private RubblesEventController _controller;
        private Button _goAroundButton;
        private Button _clearRubbleButton;

        private List<GameObject> _cleanupGos;
        private List<ScriptableObject> _cleanupSos;
        private string _testRunSavePath;
        private string _testProfileSavePath;

        [SetUp]
        public void SetUp()
        {
            _cleanupGos = new List<GameObject>();
            _cleanupSos = new List<ScriptableObject>();

            CombatTestHelper.InitializeTestDatabase();
            RunSessionManager.ClearAll();

            _testRunSavePath = Path.Combine(Application.temporaryCachePath, "rubbles_test_run.dat");
            _testProfileSavePath = Path.Combine(Application.temporaryCachePath, "rubbles_test_profile.dat");
            SaveManager.SetSavePathsForTesting(_testRunSavePath, _testProfileSavePath);

            if (File.Exists(_testRunSavePath)) File.Delete(_testRunSavePath);
            if (File.Exists(_testProfileSavePath)) File.Delete(_testProfileSavePath);

            _uiRoot = new GameObject("RubblesEventUI");
            _cleanupGos.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<RubblesEventController>();

            var goAroundGo = new GameObject("GoAroundButton");
            goAroundGo.transform.SetParent(_uiRoot.transform);
            _goAroundButton = goAroundGo.AddComponent<Button>();

            var clearRubbleGo = new GameObject("ClearRubbleButton");
            clearRubbleGo.transform.SetParent(_uiRoot.transform);
            _clearRubbleButton = clearRubbleGo.AddComponent<Button>();

            SetPrivateField("goAroundButton", _goAroundButton);
            SetPrivateField("clearRubbleButton", _clearRubbleButton);

            var onEnable = typeof(RubblesEventController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(_controller, null);

            _controller.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanupGos)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            foreach (var so in _cleanupSos)
            {
                if (so != null && !UnityEditor.EditorUtility.IsPersistent(so))
                {
                    ScriptableObject.DestroyImmediate(so, true);
                }
            }

            if (!string.IsNullOrEmpty(_testRunSavePath) && File.Exists(_testRunSavePath))
            {
                File.Delete(_testRunSavePath);
            }

            if (!string.IsNullOrEmpty(_testProfileSavePath) && File.Exists(_testProfileSavePath))
            {
                File.Delete(_testProfileSavePath);
            }

            SaveManager.SetSavePathsForTesting(null, null);
            if (RunSessionManager.CurrentParty == null)
            {
                RunSessionManager.CurrentParty = new List<PartyMemberInfo>();
            }
            RunSessionManager.ClearAll();
            CombatTestHelper.CleanupTestDatabase();
        }

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(RubblesEventController)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_controller, value);
        }

        private CombatUI CreateDummyCombatUI(out GameObject battleEndPanel)
        {
            var combatUiGo = new GameObject("CombatUI");
            _cleanupGos.Add(combatUiGo);
            var combatUI = combatUiGo.AddComponent<CombatUI>();

            battleEndPanel = new GameObject("BattleEndPanel");
            battleEndPanel.transform.SetParent(combatUiGo.transform);
            battleEndPanel.SetActive(false);
            combatUI.battleEndPanel = battleEndPanel;

            return combatUI;
        }

        private PartyMemberInfo CreatePartyMember(string id, int maxHP, int? currentHP = null)
        {
            var stats = CombatTestHelper.CreateStatBlock(maxHP: maxHP);
            _cleanupSos.Add(stats);

            var character = CombatTestHelper.CreateCharacterData(id, id, stats);
            _cleanupSos.Add(character);

            return new PartyMemberInfo
            {
                character = character,
                currentLevel = 1,
                currentHP = currentHP
            };
        }

        [Test]
        public void OnGoAroundClicked_IncreasesExhaustionByConfiguredAmount()
        {
            CreateDummyCombatUI(out _);
            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 1;
            _controller.ExhaustionIncrease = 3;

            _goAroundButton.onClick.Invoke();

            Assert.AreEqual(4, RunSessionManager.ExhaustionRound);
            Assert.IsFalse(_uiRoot.activeSelf);
            Assert.IsFalse(_goAroundButton.interactable);
            Assert.IsFalse(_clearRubbleButton.interactable);
        }

        [Test]
        public void OnGoAroundClicked_ClampsExhaustionAtLimit()
        {
            CreateDummyCombatUI(out _);
            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 7;
            _controller.ExhaustionIncrease = 3;

            _goAroundButton.onClick.Invoke();

            Assert.AreEqual(8, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(RunSessionManager.IsExhausted);
        }

        [Test]
        public void OnGoAroundClicked_WhenExhaustionLimitIsZero_IsNoOp()
        {
            CreateDummyCombatUI(out _);
            RunSessionManager.ExhaustionLimit = 0;
            RunSessionManager.ExhaustionRound = 0;
            _controller.ExhaustionIncrease = 2;

            _goAroundButton.onClick.Invoke();

            Assert.AreEqual(0, RunSessionManager.ExhaustionRound);
            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnClearRubbleClicked_DamagesAllPartyMembersByPercentageOfMaxHP()
        {
            CreateDummyCombatUI(out _);

            var memberA = CreatePartyMember("A", maxHP: 100, currentHP: null); // full HP
            var memberB = CreatePartyMember("B", maxHP: 80, currentHP: 60);    // already damaged
            var party = new List<PartyMemberInfo> { memberA, memberB };

            _controller.Initialize(party);
            _controller.DamagePercent = 0.25f;

            _clearRubbleButton.onClick.Invoke();

            // memberA: 100 - 25 = 75
            Assert.AreEqual(75, memberA.currentHP);
            // memberB: 60 - round(80 * 0.25) = 60 - 20 = 40
            Assert.AreEqual(40, memberB.currentHP);
            Assert.IsFalse(_uiRoot.activeSelf);
            Assert.IsFalse(_goAroundButton.interactable);
            Assert.IsFalse(_clearRubbleButton.interactable);
        }

        [Test]
        public void OnClearRubbleClicked_WhenDamageWouldKill_LeavesMemberAtOneHP()
        {
            CreateDummyCombatUI(out _);

            var memberA = CreatePartyMember("A", maxHP: 100, currentHP: 15);
            var memberB = CreatePartyMember("B", maxHP: 100, currentHP: null);
            var party = new List<PartyMemberInfo> { memberA, memberB };

            _controller.Initialize(party);
            _controller.DamagePercent = 1.0f;

            _clearRubbleButton.onClick.Invoke();

            Assert.AreEqual(1, memberA.currentHP);
            Assert.AreEqual(1, memberB.currentHP);
        }

        [Test]
        public void OnClearRubbleClicked_WhenMemberAlreadyAtOneHP_StaysAtOneHP()
        {
            CreateDummyCombatUI(out _);

            var member = CreatePartyMember("A", maxHP: 100, currentHP: 1);
            var party = new List<PartyMemberInfo> { member };

            _controller.Initialize(party);
            _controller.DamagePercent = 0.5f;

            _clearRubbleButton.onClick.Invoke();

            Assert.AreEqual(1, member.currentHP);
        }

        [Test]
        public void OnClearRubbleClicked_WhenDamagePercentIsZero_KeepsFullHPAsNull()
        {
            CreateDummyCombatUI(out _);

            var fullMember = CreatePartyMember("Full", maxHP: 100, currentHP: null);
            var hurtMember = CreatePartyMember("Hurt", maxHP: 100, currentHP: 50);
            var party = new List<PartyMemberInfo> { fullMember, hurtMember };

            _controller.Initialize(party);
            _controller.DamagePercent = 0f;

            _clearRubbleButton.onClick.Invoke();

            Assert.IsNull(fullMember.currentHP);
            Assert.AreEqual(50, hurtMember.currentHP);
        }

        [Test]
        public void OnClearRubbleClicked_HandlesNullPartyAndNullMembersSafely()
        {
            CreateDummyCombatUI(out _);

            var validMember = CreatePartyMember("Valid", maxHP: 100, currentHP: null);
            var memberMissingCharacter = new PartyMemberInfo { character = null, currentHP = 10 };
            var party = new List<PartyMemberInfo> { null, memberMissingCharacter, validMember };

            _controller.Initialize(party);
            _controller.DamagePercent = 0.25f;

            Assert.DoesNotThrow(() => _clearRubbleButton.onClick.Invoke());
            Assert.AreEqual(75, validMember.currentHP);
            Assert.AreEqual(10, memberMissingCharacter.currentHP);

            // Also verify null RunSessionManager.CurrentParty does not throw
            _uiRoot.SetActive(true);
            var previousParty = RunSessionManager.CurrentParty;
            try
            {
                RunSessionManager.CurrentParty = null;
                _controller.Initialize(null);
                Assert.DoesNotThrow(() => _controller.OnClearRubbleClicked());
            }
            finally
            {
                RunSessionManager.CurrentParty = previousParty ?? new List<PartyMemberInfo>();
            }
        }

        [Test]
        public void CompleteRoom_WhenCombatUIPresent_DeactivatesRoom()
        {
            CreateDummyCombatUI(out var battleEndPanel);

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
            Assert.IsTrue(battleEndPanel.activeSelf, "CombatUI.ShowRoomSelectionImmediately should activate battleEndPanel.");
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[RubblesEventController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnDisable_RemovesButtonListeners()
        {
            CreateDummyCombatUI(out _);
            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 0;

            var member = CreatePartyMember("A", maxHP: 100, currentHP: null);
            _controller.Initialize(new List<PartyMemberInfo> { member });

            var onDisable = typeof(RubblesEventController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable?.Invoke(_controller, null);

            _goAroundButton.onClick.Invoke();
            Assert.AreEqual(0, RunSessionManager.ExhaustionRound, "Go Around click should do nothing after OnDisable.");
            Assert.IsTrue(_uiRoot.activeSelf);

            _clearRubbleButton.onClick.Invoke();
            Assert.IsNull(member.currentHP, "Clear Rubble click should do nothing after OnDisable.");
            Assert.IsTrue(_uiRoot.activeSelf);
        }

        [Test]
        public void ComputeDamagedHP_BoundaryValues()
        {
            // 0% damage leaves HP unchanged
            Assert.AreEqual(100, RubblesEventController.ComputeDamagedHP(100, 100, 0f));

            // 25% of 100 = 25 -> 75
            Assert.AreEqual(75, RubblesEventController.ComputeDamagedHP(100, 100, 0.25f));

            // 50% of 101 = 50.5 -> Mathf.RoundToInt(50.5f) rounds to even = 50 -> 101 - 50 = 51
            Assert.AreEqual(51, RubblesEventController.ComputeDamagedHP(101, 101, 0.5f));

            // 100% damage clamps at 1 HP
            Assert.AreEqual(1, RubblesEventController.ComputeDamagedHP(100, 100, 1.0f));

            // Out-of-range percentage > 1 clamps to 1.0 (still leaves 1 HP)
            Assert.AreEqual(1, RubblesEventController.ComputeDamagedHP(100, 100, 2.0f));

            // Negative percentage clamps to 0 (no heal)
            Assert.AreEqual(50, RubblesEventController.ComputeDamagedHP(50, 100, -0.5f));

            // Already at 1 HP or 0 HP stays unchanged
            Assert.AreEqual(1, RubblesEventController.ComputeDamagedHP(1, 100, 0.5f));
            Assert.AreEqual(0, RubblesEventController.ComputeDamagedHP(0, 100, 0.5f));
        }
    }
}
