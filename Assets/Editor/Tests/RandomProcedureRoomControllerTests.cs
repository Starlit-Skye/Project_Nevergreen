using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class RandomProcedureRoomControllerTests
    {
        private class MockRandom : System.Random
        {
            public Queue<int> NextInts = new Queue<int>();
            public double NextDoubleValue = 0.0;

            public override int Next(int maxValue)
            {
                if (NextInts.Count > 0)
                {
                    return Mathf.Clamp(NextInts.Dequeue(), 0, Mathf.Max(0, maxValue - 1));
                }
                return 0;
            }

            public override double NextDouble()
            {
                return NextDoubleValue;
            }
        }

        private GameObject _uiRoot;
        private RandomProcedureRoomController _controller;

        private GameObject _procedurePanel;
        private Button _procedureButton1;
        private Image _procedureButton1Image;
        private Button _procedureButton2;
        private Image _procedureButton2Image;

        private GameObject _selectionPanel;
        private Transform _choicesContainer;
        private Button _templateChoiceButton;
        private Button _confirmButton;

        private TextMeshProUGUI _infoNameText;
        private TextMeshProUGUI _coreStatsText;
        private TextMeshProUGUI _resistancesText;
        private Transform _perfectionsContainer;
        private Transform _imperfectionsContainer;
        private GameObject _perfectionPrefab;
        private GameObject _imperfectionPrefab;

        private TraitData _perf1;
        private TraitData _perf2;
        private TraitData _perf3;
        private TraitData _perf4;
        private TraitData _imperf1;
        private TraitData _imperf2;
        private TraitData _imperf3;
        private TraitData _imperf4;

        private GlobalConfig _globalConfig;
        private TraitDatabase _traitDatabase;
        private GameDatabase _gameDatabase;

        private List<PartyMemberInfo> _party;
        private PartyMemberInfo _member1;
        private PartyMemberInfo _ceciliaMember;

        private MockRandom _mockRng;
        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
            _mockRng = new MockRandom();

            // Create traits
            _perf1 = CreateTrait("P1", "Sharp Eyes", TraitType.Perfection);
            _perf2 = CreateTrait("P2", "Iron Grip", TraitType.Perfection);
            _perf3 = CreateTrait("P3", "Swift Step", TraitType.Perfection);
            _perf4 = CreateTrait("P4", "Calm Mind", TraitType.Perfection);

            _imperf1 = CreateTrait("I1", "Blurred Vision", TraitType.Imperfection);
            _imperf2 = CreateTrait("I2", "Weak Grip", TraitType.Imperfection);
            _imperf3 = CreateTrait("I3", "Heavy Feet", TraitType.Imperfection);
            _imperf4 = CreateTrait("I4", "Panic", TraitType.Imperfection);

            // Set opposite traits between P1 and I1
            _perf1.oppositeTrait = _imperf1;
            _imperf1.oppositeTrait = _perf1;

            _globalConfig = ScriptableObject.CreateInstance<GlobalConfig>();
            _globalConfig.maxPerfections = 3;
            _globalConfig.maxImperfections = 3;
            _objectsToCleanUp.Add(_globalConfig);

            _traitDatabase = ScriptableObject.CreateInstance<TraitDatabase>();
            _traitDatabase.perfections = new List<TraitData> { _perf1, _perf2, _perf3, _perf4 };
            _traitDatabase.imperfections = new List<TraitData> { _imperf1, _imperf2, _imperf3, _imperf4 };
            _objectsToCleanUp.Add(_traitDatabase);

            _gameDatabase = GameDatabase.CreateForTesting(globalCfg: _globalConfig, traits: _traitDatabase);
            _objectsToCleanUp.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Create party members
            _member1 = CreatePartyMember("doll_1", "Automaton", 2);
            _ceciliaMember = CreatePartyMember("ceci", "Cecilia", 3);
            _party = new List<PartyMemberInfo> { _member1, _ceciliaMember };

            // Prefabs for info panel badges
            _perfectionPrefab = new GameObject("PerfectionItemPrefab");
            var perfLabelGo = new GameObject("Label");
            perfLabelGo.transform.SetParent(_perfectionPrefab.transform);
            perfLabelGo.AddComponent<TextMeshProUGUI>();
            _objectsToCleanUp.Add(_perfectionPrefab);

            _imperfectionPrefab = new GameObject("ImperfectionItemPrefab");
            var imperfLabelGo = new GameObject("Label");
            imperfLabelGo.transform.SetParent(_imperfectionPrefab.transform);
            imperfLabelGo.AddComponent<TextMeshProUGUI>();
            _objectsToCleanUp.Add(_imperfectionPrefab);

            // Build UI hierarchy
            _uiRoot = new GameObject("RandomProcedureRoomUI");
            _objectsToCleanUp.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<RandomProcedureRoomController>();
            SetPrivateField("_rng", _mockRng);

            _procedurePanel = new GameObject("ProcedurePanel");
            _procedurePanel.transform.SetParent(_uiRoot.transform);

            var procBtn1Go = new GameObject("ProcedureButton1");
            procBtn1Go.transform.SetParent(_procedurePanel.transform);
            _procedureButton1Image = procBtn1Go.AddComponent<Image>();
            _procedureButton1 = procBtn1Go.AddComponent<Button>();

            var procBtn2Go = new GameObject("ProcedureButton2");
            procBtn2Go.transform.SetParent(_procedurePanel.transform);
            _procedureButton2Image = procBtn2Go.AddComponent<Image>();
            _procedureButton2 = procBtn2Go.AddComponent<Button>();

            _selectionPanel = new GameObject("SelectionPanel");
            _selectionPanel.transform.SetParent(_uiRoot.transform);

            var choicesGo = new GameObject("ChoicesContainer");
            choicesGo.transform.SetParent(_selectionPanel.transform);
            _choicesContainer = choicesGo.transform;

            var templateBtnGo = new GameObject("TemplateChoiceButton");
            templateBtnGo.transform.SetParent(_choicesContainer);
            templateBtnGo.AddComponent<Image>();
            _templateChoiceButton = templateBtnGo.AddComponent<Button>();
            var templateTxtGo = new GameObject("Text");
            templateTxtGo.transform.SetParent(templateBtnGo.transform);
            templateTxtGo.AddComponent<TextMeshProUGUI>();

            var confirmBtnGo = new GameObject("ConfirmButton");
            confirmBtnGo.transform.SetParent(_selectionPanel.transform);
            _confirmButton = confirmBtnGo.AddComponent<Button>();

            _infoNameText = CreateText(_selectionPanel.transform, "InfoNameText");
            _coreStatsText = CreateText(_selectionPanel.transform, "CoreStatsText");
            _resistancesText = CreateText(_selectionPanel.transform, "ResistancesText");

            var perfContainerGo = new GameObject("PerfectionsContainer");
            perfContainerGo.transform.SetParent(_selectionPanel.transform);
            _perfectionsContainer = perfContainerGo.transform;

            var imperfContainerGo = new GameObject("ImperfectionsContainer");
            imperfContainerGo.transform.SetParent(_selectionPanel.transform);
            _imperfectionsContainer = imperfContainerGo.transform;

            // Inject serialized fields
            SetPrivateField("procedurePanel", _procedurePanel);
            SetPrivateField("procedureButton1", _procedureButton1);
            SetPrivateField("procedureButton2", _procedureButton2);
            SetPrivateField("selectionPanel", _selectionPanel);
            SetPrivateField("choiceButtons", new Button[] { _templateChoiceButton });
            SetPrivateField("confirmButton", _confirmButton);
            SetPrivateField("infoNameText", _infoNameText);
            SetPrivateField("coreStatsText", _coreStatsText);
            SetPrivateField("resistancesText", _resistancesText);
            SetPrivateField("perfectionsContainer", _perfectionsContainer);
            SetPrivateField("imperfectionsContainer", _imperfectionsContainer);
            SetPrivateField("perfectionUIItemPrefab", _perfectionPrefab);
            SetPrivateField("imperfectionUIItemPrefab", _imperfectionPrefab);

            // Invoke OnEnable
            var onEnable = typeof(RandomProcedureRoomController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(_controller, null);

            _controller.Initialize(_party);
        }

        [TearDown]
        public void TearDown()
        {
            GameDatabase.SetInstanceForTesting(null);

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

        private void SetPrivateField(string fieldName, object value)
        {
            typeof(RandomProcedureRoomController)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_controller, value);
        }

        private TraitData CreateTrait(string id, string name, TraitType type)
        {
            var t = ScriptableObject.CreateInstance<TraitData>();
            t.traitId = id;
            t.displayName = name;
            t.traitType = type;
            _objectsToCleanUp.Add(t);
            return t;
        }

        private PartyMemberInfo CreatePartyMember(string id, string name, int level)
        {
            var charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = id;
            charData.displayName = name;

            for (int i = 0; i < level; i++)
            {
                var stat = ScriptableObject.CreateInstance<StatBlockData>();
                stat.maxHP = 100 + i * 10;
                stat.attack = 20 + i * 2;
                stat.defense = 10;
                stat.speed = 5;
                stat.accuracy = 90;
                stat.dodge = 15;
                stat.critChance = 10;
                stat.bleedResist = 25;
                stat.blightResist = 30;
                stat.stunResist = 35;
                stat.debuffResist = 40;
                stat.moveResist = 45;
                _objectsToCleanUp.Add(stat);
                charData.statPerLevel.Add(stat);
            }

            _objectsToCleanUp.Add(charData);

            return new PartyMemberInfo
            {
                character = charData,
                currentLevel = level,
                perfections = new List<TraitData>(),
                imperfections = new List<TraitData>()
            };
        }

        private TextMeshProUGUI CreateText(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.AddComponent<TextMeshProUGUI>();
        }

        [Test]
        public void Initialize_SetsDefaultState_AndKeepsProcedurePanelVisible()
        {
            Assert.AreEqual(RandomProcedureRoomController.ProcedureType.None, _controller.ChosenProcedure);
            Assert.AreEqual(-1, _controller.SelectedMarionetteIndex);
            Assert.IsTrue(_procedurePanel.activeSelf, "Procedure panel should remain visible.");
            Assert.IsFalse(_selectionPanel.activeSelf, "Selection panel should be hidden initially.");
            Assert.IsFalse(_confirmButton.interactable, "Confirm button should be disabled initially.");
        }

        [Test]
        public void ProcedureButtons_StoreRememberedChoice_KeepTwoButtonViewVisible_AndShowSelectionPanel()
        {
            _procedureButton1.onClick.Invoke();

            Assert.AreEqual(RandomProcedureRoomController.ProcedureType.LoseTrait, _controller.ChosenProcedure);
            Assert.IsTrue(_procedurePanel.activeSelf, "Two-button view must remain visible after clicking procedure 1.");
            Assert.IsTrue(_selectionPanel.activeSelf, "Selection panel should open after clicking procedure 1.");
            Assert.AreEqual(Color.yellow, _procedureButton1Image.color);
            Assert.AreEqual(Color.white, _procedureButton2Image.color);

            // Switch to Procedure 2
            _procedureButton2.onClick.Invoke();

            Assert.AreEqual(RandomProcedureRoomController.ProcedureType.Gamble, _controller.ChosenProcedure);
            Assert.IsTrue(_procedurePanel.activeSelf, "Two-button view must remain visible after clicking procedure 2.");
            Assert.AreEqual(Color.white, _procedureButton1Image.color);
            Assert.AreEqual(Color.yellow, _procedureButton2Image.color);
        }

        [Test]
        public void PopulateChoiceButtons_IncludesAllPartyMembers_IncludingCecilia()
        {
            _controller.OnProcedure1Clicked();

            // Template button (inactive) + 2 instantiated buttons (active)
            var activeButtons = new List<Button>();
            foreach (Transform child in _choicesContainer)
            {
                if (child.gameObject.activeSelf)
                {
                    activeButtons.Add(child.GetComponent<Button>());
                }
            }

            Assert.AreEqual(2, activeButtons.Count);
            Assert.AreEqual("Automaton", activeButtons[0].GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.AreEqual("Cecilia", activeButtons[1].GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.IsTrue(activeButtons[0].interactable);
            Assert.IsTrue(activeButtons[1].interactable, "Cecilia must be selectable.");
        }

        [Test]
        public void SelectingMarionette_PopulatesInfoPanel_AndEnablesConfirmButton()
        {
            _ceciliaMember.perfections.Add(_perf2);
            _ceciliaMember.imperfections.Add(_imperf2);

            _controller.OnProcedure1Clicked();
            Assert.IsFalse(_confirmButton.interactable);

            // Select Cecilia (index 1)
            _controller.OnMarionetteChoiceClicked(1);

            Assert.AreEqual(1, _controller.SelectedMarionetteIndex);
            Assert.IsTrue(_confirmButton.interactable);
            Assert.AreEqual("Cecilia - LV 3", _infoNameText.text);
            StringAssert.Contains("Max HP: 120", _coreStatsText.text);
            StringAssert.Contains("Attack: 24", _coreStatsText.text);
            StringAssert.Contains("Bleed Res: 25%", _resistancesText.text);

            Assert.AreEqual(1, _perfectionsContainer.childCount);
            Assert.AreEqual("- Iron Grip", _perfectionsContainer.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.IsNotNull(_perfectionsContainer.GetChild(0).GetComponent<TraitTooltipTrigger>());

            Assert.AreEqual(1, _imperfectionsContainer.childCount);
            Assert.AreEqual("- Weak Grip", _imperfectionsContainer.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.IsNotNull(_imperfectionsContainer.GetChild(0).GetComponent<TraitTooltipTrigger>());
        }

        [Test]
        public void ApplyLoseTrait_WhenNeitherTraitPresent_DoesNothing()
        {
            _controller.ApplyLoseTrait(_member1);

            Assert.AreEqual(0, _member1.perfections.Count);
            Assert.AreEqual(0, _member1.imperfections.Count);
        }

        [Test]
        public void ApplyLoseTrait_WhenOnlyPerfectionPresent_RemovesPerfection()
        {
            _member1.perfections.Add(_perf1);

            _controller.ApplyLoseTrait(_member1);

            Assert.AreEqual(0, _member1.perfections.Count);
        }

        [Test]
        public void ApplyLoseTrait_WhenOnlyImperfectionPresent_RemovesImperfection()
        {
            _member1.imperfections.Add(_imperf1);

            _controller.ApplyLoseTrait(_member1);

            Assert.AreEqual(0, _member1.imperfections.Count);
        }

        [Test]
        public void ApplyLoseTrait_WhenBothPresent_Uses5050Roll()
        {
            // Roll 0 -> Perfection, index 0
            _member1.perfections.Add(_perf1);
            _member1.imperfections.Add(_imperf2);
            _mockRng.NextInts.Enqueue(0); // Choose Perfection
            _mockRng.NextInts.Enqueue(0); // Remove index 0

            _controller.ApplyLoseTrait(_member1);

            Assert.AreEqual(0, _member1.perfections.Count);
            Assert.AreEqual(1, _member1.imperfections.Count);

            // Roll 1 -> Imperfection, index 0
            _member1.perfections.Add(_perf1);
            _mockRng.NextInts.Enqueue(1); // Choose Imperfection
            _mockRng.NextInts.Enqueue(0); // Remove index 0

            _controller.ApplyLoseTrait(_member1);

            Assert.AreEqual(1, _member1.perfections.Count);
            Assert.AreEqual(0, _member1.imperfections.Count);
        }

        [Test]
        public void ApplyGamble_BadOutcome_RemovesPerfectionAndAddsImperfection_RespectingDuplicateAndOppositeRules()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.75; // >= 0.5 -> Bad outcome

            // Member has P1 (opposite of I1), P2 (to be removed), and I2 (duplicate of I2)
            // After removing P2 (index 1), P1 remains so I1 is still blocked, and I2 is duplicate -> valid candidates: I3, I4
            _member1.perfections.AddRange(new[] { _perf1, _perf2 });
            _member1.imperfections.Add(_imperf2);

            _mockRng.NextInts.Enqueue(1); // Remove index 1 (P2)
            _mockRng.NextInts.Enqueue(0); // Pick first valid candidate (I3)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(1, _member1.perfections.Count);
            Assert.AreEqual(_perf1, _member1.perfections[0]);
            Assert.AreEqual(2, _member1.imperfections.Count);
            Assert.Contains(_imperf3, _member1.imperfections);
            Assert.IsFalse(_member1.imperfections.Contains(_imperf1), "Must not add opposite trait I1.");
        }

        [Test]
        public void ApplyGamble_BadOutcome_SkipsRemovalWhenNoPerfections_AndStillGainsImperfection()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.8; // Bad outcome

            _mockRng.NextInts.Enqueue(0); // Pick candidate 0 (I1)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(0, _member1.perfections.Count);
            Assert.AreEqual(1, _member1.imperfections.Count);
            Assert.AreEqual(_imperf1, _member1.imperfections[0]);
        }

        [Test]
        public void ApplyGamble_BadOutcome_RemovesPerfection_AndSkipsGainWhenImperfectionsFull()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.9; // Bad outcome

            _member1.perfections.Add(_perf2);
            _member1.imperfections.AddRange(new[] { _imperf1, _imperf2, _imperf3 });
            _mockRng.NextInts.Enqueue(0); // Remove index 0 (P2)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(0, _member1.perfections.Count);
            Assert.AreEqual(3, _member1.imperfections.Count);
        }

        [Test]
        public void ApplyGamble_GoodOutcome_RemovesImperfectionAndGainsPerfection()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.25; // < 0.5 -> Good outcome

            // Start with I1 (which blocks P1). Removing I1 first unblocks P1!
            _member1.imperfections.Add(_imperf1);
            _mockRng.NextInts.Enqueue(0); // Remove index 0 (I1)
            _mockRng.NextInts.Enqueue(0); // Add candidate 0 (now P1 is unblocked!)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(0, _member1.imperfections.Count);
            Assert.AreEqual(1, _member1.perfections.Count);
            Assert.AreEqual(_perf1, _member1.perfections[0]);
        }

        [Test]
        public void ApplyGamble_GoodOutcome_SkipsRemovalWhenNoImperfections_AndStillGainsPerfection()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.1; // Good outcome

            _mockRng.NextInts.Enqueue(0); // Add candidate 0 (P1)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(0, _member1.imperfections.Count);
            Assert.AreEqual(1, _member1.perfections.Count);
            Assert.AreEqual(_perf1, _member1.perfections[0]);
        }

        [Test]
        public void ApplyGamble_GoodOutcome_RemovesImperfection_AndSkipsGainWhenPerfectionsFull()
        {
            _controller.GoodOutcomeChance = 0.5f;
            _mockRng.NextDoubleValue = 0.1; // Good outcome

            _member1.perfections.AddRange(new[] { _perf2, _perf3, _perf4 });
            _member1.imperfections.Add(_imperf2);
            _mockRng.NextInts.Enqueue(0); // Remove index 0 (I2)

            _controller.ApplyGamble(_member1);

            Assert.AreEqual(0, _member1.imperfections.Count);
            Assert.AreEqual(3, _member1.perfections.Count);
        }

        [Test]
        public void OnConfirmClicked_AppliesRememberedProcedureToSelectedMarionette_AndCompletesRoom()
        {
            var dummyCombatUI = new GameObject("CombatUI").AddComponent<CombatUI>();
            _objectsToCleanUp.Add(dummyCombatUI.gameObject);

            _ceciliaMember.imperfections.Add(_imperf2);

            _controller.OnProcedure1Clicked();
            _controller.OnMarionetteChoiceClicked(1); // Select Cecilia
            _confirmButton.onClick.Invoke();

            Assert.AreEqual(0, _ceciliaMember.imperfections.Count, "Procedure 1 should have removed Cecilia's imperfection.");
            Assert.IsFalse(_uiRoot.activeSelf, "Room should be deactivated on completion.");
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[RandomProcedureRoomController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnDisable_RemovesButtonListeners()
        {
            var onDisable = typeof(RandomProcedureRoomController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable?.Invoke(_controller, null);

            _procedureButton1.onClick.Invoke();
            Assert.AreEqual(RandomProcedureRoomController.ProcedureType.None, _controller.ChosenProcedure);
        }
    }
}

