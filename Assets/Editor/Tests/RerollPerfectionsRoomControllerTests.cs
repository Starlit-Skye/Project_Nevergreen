using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.UI;
using Nevergreen.Prototype;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class RerollPerfectionsRoomControllerTests
    {
        private class MockRandom : System.Random
        {
            public Queue<int> NextInts = new Queue<int>();

            public override int Next(int maxValue)
            {
                if (NextInts.Count > 0)
                {
                    return Mathf.Clamp(NextInts.Dequeue(), 0, Mathf.Max(0, maxValue - 1));
                }
                return 0;
            }
        }

        private GameObject _uiRoot;
        private RerollPerfectionsRoomController _controller;

        private Button[] _marionetteButtons;
        private Image[] _marionetteButtonImages;
        private TextMeshProUGUI[] _marionetteButtonTexts;

        private Button _rerollButton;
        private TextMeshProUGUI _rerollButtonText;
        private Button _leaveButton;

        private GameObject _perfectionsPanel;
        private Transform _perfectionsContainer;
        private GameObject _perfectionPrefab;
        private GameObject _hpTextPanel;
        private TextMeshProUGUI _hpText;

        private TraitData _perf1;
        private TraitData _perf2;
        private TraitData _perf3;
        private TraitData _perf4;
        private TraitData _perf5;
        private TraitData _perfMaxHpBuff;
        private TraitData _imperf1;

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

            // Setup Traits
            _perf1 = CreateTrait("P1", "Sharp Eyes", TraitType.Perfection);
            _perf2 = CreateTrait("P2", "Iron Grip", TraitType.Perfection);
            _perf3 = CreateTrait("P3", "Swift Step", TraitType.Perfection);
            _perf4 = CreateTrait("P4", "Calm Mind", TraitType.Perfection);
            _perf5 = CreateTrait("P5", "Keen Reflex", TraitType.Perfection);
            _perfMaxHpBuff = CreateMaxHpBuffTrait("P_HP", "Titan Frame", 50); // +50% Max HP

            _imperf1 = CreateTrait("I1", "Blurred Vision", TraitType.Imperfection);

            // Opposite relationship between P3 and I1
            _perf3.oppositeTrait = _imperf1;
            _imperf1.oppositeTrait = _perf3;

            _globalConfig = ScriptableObject.CreateInstance<GlobalConfig>();
            _globalConfig.maxPerfections = 3;
            _globalConfig.maxImperfections = 3;
            _objectsToCleanUp.Add(_globalConfig);

            _traitDatabase = ScriptableObject.CreateInstance<TraitDatabase>();
            _traitDatabase.perfections = new List<TraitData> { _perf1, _perf2, _perf3, _perf4, _perf5 };
            _traitDatabase.imperfections = new List<TraitData> { _imperf1 };
            _objectsToCleanUp.Add(_traitDatabase);

            _gameDatabase = GameDatabase.CreateForTesting(globalCfg: _globalConfig, traits: _traitDatabase);
            _objectsToCleanUp.Add(_gameDatabase);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            // Create party members (2 members for 4 buttons by default)
            _member1 = CreatePartyMember("doll_1", "Automaton", 100);
            _ceciliaMember = CreatePartyMember("ceci", "Cecilia", 200);
            _party = new List<PartyMemberInfo> { _member1, _ceciliaMember };

            // Perfection badge prefab
            _perfectionPrefab = new GameObject("PerfectionItemPrefab");
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_perfectionPrefab.transform);
            labelGo.AddComponent<TextMeshProUGUI>();
            _objectsToCleanUp.Add(_perfectionPrefab);

            // Build UI hierarchy
            _uiRoot = new GameObject("RerollPerfectionsRoomUI");
            _objectsToCleanUp.Add(_uiRoot);
            _controller = _uiRoot.AddComponent<RerollPerfectionsRoomController>();
            SetPrivateField("_rng", _mockRng);

            _marionetteButtons = new Button[4];
            _marionetteButtonImages = new Image[4];
            _marionetteButtonTexts = new TextMeshProUGUI[4];

            for (int i = 0; i < 4; i++)
            {
                var btnGo = new GameObject($"MarionetteButton_{i}");
                btnGo.transform.SetParent(_uiRoot.transform);
                _marionetteButtonImages[i] = btnGo.AddComponent<Image>();
                _marionetteButtons[i] = btnGo.AddComponent<Button>();

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(btnGo.transform);
                _marionetteButtonTexts[i] = txtGo.AddComponent<TextMeshProUGUI>();
            }

            var rerollBtnGo = new GameObject("RerollButton");
            rerollBtnGo.transform.SetParent(_uiRoot.transform);
            _rerollButton = rerollBtnGo.AddComponent<Button>();
            var rerollTxtGo = new GameObject("Text");
            rerollTxtGo.transform.SetParent(rerollBtnGo.transform);
            _rerollButtonText = rerollTxtGo.AddComponent<TextMeshProUGUI>();

            var leaveBtnGo = new GameObject("LeaveButton");
            leaveBtnGo.transform.SetParent(_uiRoot.transform);
            _leaveButton = leaveBtnGo.AddComponent<Button>();

            _perfectionsPanel = new GameObject("PerfectionsPanel");
            _perfectionsPanel.transform.SetParent(_uiRoot.transform);
            var perfContGo = new GameObject("PerfectionsContainer");
            perfContGo.transform.SetParent(_perfectionsPanel.transform);
            _perfectionsContainer = perfContGo.transform;

            _hpTextPanel = new GameObject("HPTextPanel");
            _hpTextPanel.transform.SetParent(_uiRoot.transform);
            var hpTxtGo = new GameObject("HPText");
            hpTxtGo.transform.SetParent(_hpTextPanel.transform);
            _hpText = hpTxtGo.AddComponent<TextMeshProUGUI>();

            // Inject serialized fields
            SetPrivateField("marionetteButtons", _marionetteButtons);
            SetPrivateField("rerollButton", _rerollButton);
            SetPrivateField("leaveButton", _leaveButton);
            SetPrivateField("perfectionsPanel", _perfectionsPanel);
            SetPrivateField("perfectionsContainer", _perfectionsContainer);
            SetPrivateField("perfectionUIItemPrefab", _perfectionPrefab);
            SetPrivateField("hpTextPanel", _hpTextPanel);
            SetPrivateField("hpText", _hpText);

            // Invoke OnEnable
            var onEnable = typeof(RerollPerfectionsRoomController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
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
            typeof(RerollPerfectionsRoomController)
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

        private TraitData CreateMaxHpBuffTrait(string id, string name, int percentBonus)
        {
            var t = CreateTrait(id, name, TraitType.Perfection);
            t.effectStrategies = new List<TraitEffectStrategy>
            {
                new StatModifierTraitStrategy
                {
                    targetStat = StatTarget.MaxHP,
                    amplitudeType = AmplitudeType.Percentage,
                    amount = percentBonus
                }
            };
            return t;
        }

        private PartyMemberInfo CreatePartyMember(string id, string name, int baseMaxHP)
        {
            var charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = id;
            charData.displayName = name;

            var stat = ScriptableObject.CreateInstance<StatBlockData>();
            stat.maxHP = baseMaxHP;
            _objectsToCleanUp.Add(stat);
            charData.statPerLevel.Add(stat);
            _objectsToCleanUp.Add(charData);

            return new PartyMemberInfo
            {
                character = charData,
                currentLevel = 1,
                perfections = new List<TraitData>(),
                imperfections = new List<TraitData>(),
                equippedTrinkets = new List<TrinketData>()
            };
        }

        [Test]
        public void Initialize_SetsMemberNames_GreysOutExtraButtons_AndHidesPanels()
        {
            Assert.AreEqual("Automaton", _marionetteButtonTexts[0].text);
            Assert.IsTrue(_marionetteButtons[0].interactable);

            Assert.AreEqual("Cecilia", _marionetteButtonTexts[1].text);
            Assert.IsTrue(_marionetteButtons[1].interactable, "Cecilia must be selectable.");

            Assert.AreEqual("Empty Slot", _marionetteButtonTexts[2].text);
            Assert.IsFalse(_marionetteButtons[2].interactable, "Extra button 2 should be greyed out.");

            Assert.AreEqual("Empty Slot", _marionetteButtonTexts[3].text);
            Assert.IsFalse(_marionetteButtons[3].interactable, "Extra button 3 should be greyed out.");

            Assert.IsFalse(_perfectionsPanel.activeSelf);
            Assert.IsFalse(_hpTextPanel.activeSelf);
            Assert.IsFalse(_rerollButton.interactable);
        }

        [Test]
        public void SelectingMarionette_ActivatesPanels_UpdatesTraitModifiedHPAndPerfections_AndSetsRerollButtonText()
        {
            // Give member1 (base 100 Max HP) a +50% Max HP Perfection -> effective Max HP = 150
            _member1.perfections.Add(_perfMaxHpBuff);
            _controller.RerollDamagePercent = 0.25f; // 25% of 150 = 38 damage

            _marionetteButtons[0].onClick.Invoke();

            Assert.AreEqual(0, _controller.SelectedIndex);
            Assert.IsTrue(_perfectionsPanel.activeSelf);
            Assert.IsTrue(_hpTextPanel.activeSelf);
            Assert.AreEqual("HP: 150/150", _hpText.text);
            Assert.AreEqual(1, _perfectionsContainer.childCount);
            Assert.AreEqual("- Titan Frame", _perfectionsContainer.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text);
            Assert.IsNotNull(_perfectionsContainer.GetChild(0).GetComponent<TraitTooltipTrigger>());

            Assert.IsTrue(_rerollButton.interactable);
            Assert.AreEqual("Randomize all Perfections. -38 HP", _rerollButtonText.text);
        }

        [Test]
        public void SelectingMarionette_WithExactlyOneHpRemainingAfterDamage_EnablesRerollButton()
        {
            // Base 100 HP, 25% damage = 25. Current HP = 26 -> 26 - 25 = 1 HP remaining (survivable!)
            _member1.perfections.Add(_perf1);
            _member1.currentHP = 26;
            _controller.RerollDamagePercent = 0.25f;

            _controller.OnMarionetteClicked(0);

            Assert.IsTrue(_rerollButton.interactable, "Surviving at exactly 1 HP must enable Reroll button.");
            Assert.AreEqual("Randomize all Perfections. -25 HP", _rerollButtonText.text);
        }

        [Test]
        public void SelectingMarionette_WhenCannotSurviveReroll_DisablesRerollButton()
        {
            // Base 100 HP, 25% damage = 25. Current HP = 25 -> 25 - 25 = 0 HP (not survivable)
            _member1.perfections.Add(_perf1);
            _member1.currentHP = 25;
            _controller.RerollDamagePercent = 0.25f;

            _controller.OnMarionetteClicked(0);

            Assert.IsFalse(_rerollButton.interactable, "Dropping to 0 HP must disable Reroll button.");
            Assert.AreEqual("Randomize all Perfections. -25 HP", _rerollButtonText.text);
        }

        [Test]
        public void SelectingMarionette_WithZeroPerfections_DisablesRerollButton()
        {
            // Full HP (100), 0 Perfections -> disabled
            _controller.RerollDamagePercent = 0.25f;

            _controller.OnMarionetteClicked(0);

            Assert.IsFalse(_rerollButton.interactable, "Marionette with 0 Perfections must have Reroll button disabled.");
        }

        [Test]
        public void OnRerollClicked_DealsDamage_ReplacesAllPerfectionsExcludingOldAndOpposite_AndRefreshesUI()
        {
            // Member1 currently has P1 and P2, plus Imperfection I1 (which is opposite to P3).
            // Pool has: P1, P2, P3, P4, P5.
            // Excluded old: P1, P2. Blocked by I1: P3.
            // Valid replacements: P4 and P5!
            _member1.perfections.AddRange(new[] { _perf1, _perf2 });
            _member1.imperfections.Add(_imperf1);
            _controller.RerollDamagePercent = 0.25f; // 25 damage on 100 Max HP -> 75 HP

            _controller.OnMarionetteClicked(0);
            _rerollButton.onClick.Invoke();

            Assert.AreEqual(75, _member1.currentHP);
            Assert.AreEqual("HP: 75/100", _hpText.text);

            Assert.AreEqual(2, _member1.perfections.Count);
            Assert.Contains(_perf4, _member1.perfections);
            Assert.Contains(_perf5, _member1.perfections);
            Assert.IsFalse(_member1.perfections.Contains(_perf1), "Old perfection P1 must not reappear.");
            Assert.IsFalse(_member1.perfections.Contains(_perf2), "Old perfection P2 must not reappear.");
            Assert.IsFalse(_member1.perfections.Contains(_perf3), "Opposite perfection P3 must not be added.");
            Assert.AreEqual(2, _perfectionsContainer.childCount);
        }

        [Test]
        public void OnRerollClicked_IsRepeatableUntilMarionetteCannotSurvive()
        {
            _member1.perfections.Add(_perf1);
            _controller.RerollDamagePercent = 0.40f; // 40 damage per reroll on 100 Max HP

            _controller.OnMarionetteClicked(0);
            Assert.IsTrue(_rerollButton.interactable);

            // First reroll: 100 -> 60 HP (can still survive another 40 damage -> 20 HP)
            _rerollButton.onClick.Invoke();
            Assert.AreEqual(60, _member1.currentHP);
            Assert.IsTrue(_rerollButton.interactable, "Should remain interactable for second reroll (60 - 40 = 20 >= 1).");

            // Second reroll: 60 -> 20 HP (cannot survive 20 - 40 = -20)
            _rerollButton.onClick.Invoke();
            Assert.AreEqual(20, _member1.currentHP);
            Assert.IsFalse(_rerollButton.interactable, "Should become disabled once HP is too low to survive another reroll.");
        }

        [Test]
        public void OnRerollClicked_ClampsCurrentHpWhenMaxHpPerfectionIsLost()
        {
            // Member1 has base 100 HP + P_HP (+50% Max HP) = 150 Effective Max HP.
            // Damage = 10% of 150 = 15 -> new HP = 135.
            // After reroll, P_HP is replaced with P1 -> new Effective Max HP = 100.
            // 135 >= 100, so currentHP clamps to full (null -> displays 100/100).
            _member1.perfections.Add(_perfMaxHpBuff);
            _controller.RerollDamagePercent = 0.10f;

            _controller.OnMarionetteClicked(0);
            Assert.AreEqual("HP: 150/150", _hpText.text);
            Assert.AreEqual("Randomize all Perfections. -15 HP", _rerollButtonText.text);

            _rerollButton.onClick.Invoke();

            Assert.IsNull(_member1.currentHP, "currentHP exceeding new post-reroll Max HP should clamp to full (null).");
            Assert.AreEqual("HP: 100/100", _hpText.text);
            Assert.AreEqual("Randomize all Perfections. -10 HP", _rerollButtonText.text);
        }

        [Test]
        public void OnRerollClicked_WhenFewerValidCandidatesExist_AddsAsManyAsPossibleAndDealsDamage()
        {
            // Pool only has P1, P2, P4 (3 total).
            // Member starts with P1 and P2 (2 perfections).
            // Only P4 is available as a non-old replacement.
            _traitDatabase.perfections = new List<TraitData> { _perf1, _perf2, _perf4 };
            _member1.perfections.AddRange(new[] { _perf1, _perf2 });
            _controller.RerollDamagePercent = 0.20f; // 20 damage

            _controller.OnMarionetteClicked(0);
            _rerollButton.onClick.Invoke();

            Assert.AreEqual(80, _member1.currentHP, "Damage should still be dealt on partial reroll.");
            Assert.AreEqual(1, _member1.perfections.Count);
            Assert.AreEqual(_perf4, _member1.perfections[0]);
        }

        [Test]
        public void OnLeaveClicked_CompletesRoomFlow()
        {
            var dummyCombatUI = new GameObject("CombatUI").AddComponent<CombatUI>();
            _objectsToCleanUp.Add(dummyCombatUI.gameObject);

            _leaveButton.onClick.Invoke();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void CompleteRoom_WhenCombatUIMissing_LogsErrorAndStillDeactivates()
        {
            LogAssert.Expect(LogType.Error, "[RerollPerfectionsRoomController] CombatUI not found!");

            _controller.CompleteRoom();

            Assert.IsFalse(_uiRoot.activeSelf);
        }

        [Test]
        public void OnDisable_RemovesButtonListeners()
        {
            _member1.perfections.Add(_perf1);
            _controller.OnMarionetteClicked(0);

            var onDisable = typeof(RerollPerfectionsRoomController).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable?.Invoke(_controller, null);

            _rerollButton.onClick.Invoke();
            Assert.IsNull(_member1.currentHP, "Reroll click should do nothing after OnDisable.");

            _leaveButton.onClick.Invoke();
            Assert.IsTrue(_uiRoot.activeSelf, "Leave click should do nothing after OnDisable.");
        }
    }
}

