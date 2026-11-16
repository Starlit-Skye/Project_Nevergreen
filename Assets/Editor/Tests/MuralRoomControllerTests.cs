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
    public class MuralRoomControllerTests
    {
        private GameObject _uiRoot;
        private MuralRoomController _controller;
        private Button _destroyButton;
        private Button _gazeButton;
        private Button _leaveButton;

        private GlobalConfig _globalConfig;
        private CombatConfig _combatConfig;
        private TraitDatabase _traitDatabase;
        private GameDatabase _gameDatabase;

        private List<GameObject> _cleanupGos;
        private List<ScriptableObject> _cleanupSos;
        private string _testRunSavePath;
        private string _testProfileSavePath;

        [SetUp]
        public void SetUp()
        {
            _cleanupGos = new List<GameObject>();
            _cleanupSos = new List<ScriptableObject>();

            _globalConfig = CombatTestHelper.CreateDefaultGlobalConfig();
            _globalConfig.maxPerfections = 3;
            _globalConfig.maxImperfections = 3;
            _cleanupSos.Add(_globalConfig);

            _combatConfig = CombatTestHelper.CreateDefaultConfig();
            _cleanupSos.Add(_combatConfig);

            _traitDatabase = ScriptableObject.CreateInstance<TraitDatabase>();
            _cleanupSos.Add(_traitDatabase);

            _gameDatabase = GameDatabase.CreateForTesting(
                globalCfg: _globalConfig,
                combatCfg: _combatConfig,
                traits: _traitDatabase);
            _cleanupSos.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            RunSessionManager.ClearAll();

            _testRunSavePath = Path.Combine(Application.temporaryCachePath, "mural_test_run.dat");
            _testProfileSavePath = Path.Combine(Application.temporaryCachePath, "mural_test_profile.dat");
            SaveManager.SetSavePathsForTesting(_testRunSavePath, _testProfileSavePath);

            if (File.Exists(_testRunSavePath)) File.Delete(_testRunSavePath);
            if (File.Exists(_testProfileSavePath)) File.Delete(_testProfileSavePath);

            _uiRoot = new GameObject("MuralRoomUI");
            _cleanupGos.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<MuralRoomController>();

            var destroyGo = new GameObject("DestroyButton");
            destroyGo.transform.SetParent(_uiRoot.transform);
            _destroyButton = destroyGo.AddComponent<Button>();

            var gazeGo = new GameObject("GazeButton");
            gazeGo.transform.SetParent(_uiRoot.transform);
            _gazeButton = gazeGo.AddComponent<Button>();

            var leaveGo = new GameObject("LeaveButton");
            leaveGo.transform.SetParent(_uiRoot.transform);
            _leaveButton = leaveGo.AddComponent<Button>();
            // Leave button is disabled by default in the UI prefab
            _leaveButton.gameObject.SetActive(false);

            SetPrivateField("destroyButton", _destroyButton);
            SetPrivateField("gazeButton", _gazeButton);
            SetPrivateField("leaveButton", _leaveButton);

            var onEnable = typeof(MuralRoomController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
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
            GameDatabase.SetInstanceForTesting(null);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(MuralRoomController)
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

        private PartyMemberInfo CreatePartyMember(string id, int maxHP = 100, int? currentHP = null)
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

        private TraitData CreateTrait(string id, TraitType type, TraitData opposite = null)
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.traitId = id;
            trait.displayName = id;
            trait.traitType = type;
            trait.oppositeTrait = opposite;
            _cleanupSos.Add(trait);
            return trait;
        }

        [Test]
        public void Initialize_DoesNotChangeLeaveButtonState()
        {
            _leaveButton.gameObject.SetActive(false);
            _controller.Initialize();
            Assert.IsFalse(_leaveButton.gameObject.activeSelf, "Initialize should not enable a disabled Leave button.");

            _leaveButton.gameObject.SetActive(true);
            _controller.Initialize();
            Assert.IsTrue(_leaveButton.gameObject.activeSelf, "Initialize should not disable an active Leave button.");
        }

        [Test]
        public void OnDestroyClicked_DamagesAllPartyMembers_AndGrantsScraps()
        {
            var memberA = CreatePartyMember("A", maxHP: 100, currentHP: null);
            var memberB = CreatePartyMember("B", maxHP: 80, currentHP: 60);
            var party = new List<PartyMemberInfo> { memberA, memberB };

            _controller.Initialize(party);
            _controller.DamagePercent = 0.25f;
            _controller.ScrapReward = 50;

            _destroyButton.onClick.Invoke();

            Assert.AreEqual(75, memberA.currentHP);
            Assert.AreEqual(40, memberB.currentHP);
            Assert.AreEqual(50, RunSessionManager.Scraps);
            Assert.IsTrue(_uiRoot.activeSelf, "Room should remain open until Leave is clicked.");
        }

        [Test]
        public void OnDestroyClicked_WhenDamageWouldKill_LeavesMemberAtOneHP()
        {
            var memberA = CreatePartyMember("A", maxHP: 100, currentHP: 10);
            var memberB = CreatePartyMember("B", maxHP: 100, currentHP: null);
            var party = new List<PartyMemberInfo> { memberA, memberB };

            _controller.Initialize(party);
            _controller.DamagePercent = 1.0f;
            _controller.ScrapReward = 25;

            _destroyButton.onClick.Invoke();

            Assert.AreEqual(1, memberA.currentHP);
            Assert.AreEqual(1, memberB.currentHP);
            Assert.AreEqual(25, RunSessionManager.Scraps);
        }

        [Test]
        public void OnDestroyClicked_WhenMemberAlreadyAtOneHP_StaysAtOneHP()
        {
            var member = CreatePartyMember("A", maxHP: 100, currentHP: 1);
            _controller.Initialize(new List<PartyMemberInfo> { member });
            _controller.DamagePercent = 0.5f;

            _destroyButton.onClick.Invoke();

            Assert.AreEqual(1, member.currentHP);
        }

        [Test]
        public void OnDestroyClicked_HidesDestroyAndGaze_AndEnablesLeaveButton()
        {
            Assert.IsTrue(_destroyButton.gameObject.activeSelf);
            Assert.IsTrue(_gazeButton.gameObject.activeSelf);
            Assert.IsFalse(_leaveButton.gameObject.activeSelf);

            _destroyButton.onClick.Invoke();

            Assert.IsFalse(_destroyButton.gameObject.activeSelf);
            Assert.IsFalse(_gazeButton.gameObject.activeSelf);
            Assert.IsTrue(_leaveButton.gameObject.activeSelf);
        }

        [Test]
        public void OnGazeClicked_IncreasesExhaustion_ClampedToLimit()
        {
            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 7;
            _controller.ExhaustionIncrease = 3;

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(8, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(RunSessionManager.IsExhausted);
        }

        [Test]
        public void OnGazeClicked_WhenExhaustionLimitZero_IsNoOp()
        {
            RunSessionManager.ExhaustionLimit = 0;
            RunSessionManager.ExhaustionRound = 0;
            _controller.ExhaustionIncrease = 2;

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(0, RunSessionManager.ExhaustionRound);
        }

        [Test]
        public void OnGazeClicked_GrantsRandomPerfectionToPartyMembers()
        {
            var perf1 = CreateTrait("P1", TraitType.Perfection);
            var perf2 = CreateTrait("P2", TraitType.Perfection);
            _traitDatabase.perfections = new List<TraitData> { perf1, perf2 };

            var memberA = CreatePartyMember("A");
            var memberB = CreatePartyMember("B");
            _controller.Initialize(new List<PartyMemberInfo> { memberA, memberB });

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(1, memberA.perfections.Count);
            Assert.Contains(memberA.perfections[0], _traitDatabase.perfections);
            Assert.AreEqual(1, memberB.perfections.Count);
            Assert.Contains(memberB.perfections[0], _traitDatabase.perfections);
        }

        [Test]
        public void OnGazeClicked_RespectsMaxCap_WhenPerfectionsFull_DoesNotAdd()
        {
            _globalConfig.maxPerfections = 1;
            var existingPerf = CreateTrait("P_Existing", TraitType.Perfection);
            var newPerf = CreateTrait("P_New", TraitType.Perfection);
            _traitDatabase.perfections = new List<TraitData> { existingPerf, newPerf };

            var member = CreatePartyMember("A");
            member.perfections.Add(existingPerf);
            _controller.Initialize(new List<PartyMemberInfo> { member });

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(1, member.perfections.Count);
            Assert.AreEqual(existingPerf, member.perfections[0]);
        }

        [Test]
        public void OnGazeClicked_RespectsNoDupes_DoesNotAddExistingTraitId()
        {
            _globalConfig.maxPerfections = 3;
            var perf1 = CreateTrait("P1", TraitType.Perfection);
            var perf2 = CreateTrait("P2", TraitType.Perfection);
            _traitDatabase.perfections = new List<TraitData> { perf1, perf2 };

            var member = CreatePartyMember("A");
            member.perfections.Add(perf1);
            _controller.Initialize(new List<PartyMemberInfo> { member });

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(2, member.perfections.Count);
            Assert.AreEqual(perf1, member.perfections[0]);
            Assert.AreEqual(perf2, member.perfections[1]);
        }

        [Test]
        public void OnGazeClicked_RespectsOppositeExclusion_DoesNotAddOppositeTrait()
        {
            _globalConfig.maxPerfections = 3;
            var impOpposite = CreateTrait("I_Slow", TraitType.Imperfection);
            var perfConflictDirect = CreateTrait("P_Fast", TraitType.Perfection, opposite: impOpposite);
            var perfConflictReverse = CreateTrait("P_Reverse", TraitType.Perfection);
            var impHasReverseOpposite = CreateTrait("I_Reverse", TraitType.Imperfection, opposite: perfConflictReverse);
            var perfValid = CreateTrait("P_Valid", TraitType.Perfection);

            _traitDatabase.perfections = new List<TraitData> { perfConflictDirect, perfConflictReverse, perfValid };

            var member = CreatePartyMember("A");
            member.imperfections.Add(impOpposite);
            member.imperfections.Add(impHasReverseOpposite);
            _controller.Initialize(new List<PartyMemberInfo> { member });

            _gazeButton.onClick.Invoke();

            Assert.AreEqual(1, member.perfections.Count);
            Assert.AreEqual(perfValid, member.perfections[0]);
        }

        [Test]
        public void OnGazeClicked_HidesDestroyAndGaze_AndEnablesLeaveButton()
        {
            Assert.IsTrue(_destroyButton.gameObject.activeSelf);
            Assert.IsTrue(_gazeButton.gameObject.activeSelf);
            Assert.IsFalse(_leaveButton.gameObject.activeSelf);

            _gazeButton.onClick.Invoke();

            Assert.IsFalse(_destroyButton.gameObject.activeSelf);
            Assert.IsFalse(_gazeButton.gameObject.activeSelf);
            Assert.IsTrue(_leaveButton.gameObject.activeSelf);
        }

        [Test]
        public void OnLeaveClicked_CompletesRoomFlow()
        {
            CreateDummyCombatUI(out var battleEndPanel);

            _leaveButton.gameObject.SetActive(true);
            _leaveButton.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf);
            Assert.IsTrue(battleEndPanel.activeSelf, "CombatUI.ShowRoomSelectionImmediately should activate battleEndPanel.");
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[MuralRoomController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnDisable_RemovesListeners()
        {
            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 0;

            var onDisable = typeof(MuralRoomController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable?.Invoke(_controller, null);

            _destroyButton.onClick.Invoke();
            Assert.AreEqual(0, RunSessionManager.Scraps);
            Assert.IsTrue(_destroyButton.gameObject.activeSelf);

            _gazeButton.onClick.Invoke();
            Assert.AreEqual(0, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(_gazeButton.gameObject.activeSelf);

            _leaveButton.gameObject.SetActive(true);
            _leaveButton.onClick.Invoke();
            Assert.IsTrue(_uiRoot.activeSelf);
        }
    }
}

