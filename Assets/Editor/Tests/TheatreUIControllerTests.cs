using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.UI;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class TheatreUIControllerTests
    {
        private GameObject _controllerGo;
        private TheatreUIController _controller;
        private GameObject _buttonGo;
        private GameObject _textGo;
        private TextMeshProUGUI _textMesh;
        private GlobalConfig _globalConfig;
        private GameDatabase _gameDatabase;

        [SetUp]
        public void Setup()
        {
            _globalConfig = ScriptableObject.CreateInstance<GlobalConfig>();
            _globalConfig.theatreRoomProjectorRepairCost = 75;

            _gameDatabase = GameDatabase.CreateForTesting(globalCfg: _globalConfig);
            GameDatabase.SetInstanceForTesting(_gameDatabase);

            _controllerGo = new GameObject("TheatreUIController");
            _controller = _controllerGo.AddComponent<TheatreUIController>();

            _buttonGo = new GameObject("FixProjectorButton");
            _buttonGo.AddComponent<Button>();
            _textGo = new GameObject("ButtonText");
            _textGo.transform.SetParent(_buttonGo.transform);
            _textMesh = _textGo.AddComponent<TextMeshProUGUI>();

            _controller.fixProjectorButton = _buttonGo;
            
            // Invoke OnEnable via reflection to ensure listeners are hooked up in EditMode
            var onEnableMethod = typeof(TheatreUIController).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            GameDatabase.SetInstanceForTesting(null);

            if (_controllerGo != null)
                Object.DestroyImmediate(_controllerGo);
            if (_buttonGo != null)
                Object.DestroyImmediate(_buttonGo);
            if (_gameDatabase != null)
                Object.DestroyImmediate(_gameDatabase);
            if (_globalConfig != null)
                Object.DestroyImmediate(_globalConfig);
        }

        [Test]
        public void GlobalConfig_TheatreRoomProjectorRepairCost_DefaultIs50()
        {
            var config = ScriptableObject.CreateInstance<GlobalConfig>();
            Assert.AreEqual(50, config.theatreRoomProjectorRepairCost);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void UpdateDisplay_SetsButtonTextUsingGlobalConfig()
        {
            _controller.UpdateDisplay();
            Assert.AreEqual("Spend 75 Scraps, unlock a Marionette's skill.", _textMesh.text);
        }

        [Test]
        public void UpdateDisplay_WithExplicitCost_SetsButtonText()
        {
            _controller.UpdateDisplay(120);
            Assert.AreEqual("Spend 120 Scraps, unlock a Marionette's skill.", _textMesh.text);
        }

        [Test]
        public void UpdateDisplay_NullButton_DoesNotThrow()
        {
            _controller.fixProjectorButton = null;
            Assert.DoesNotThrow(() => _controller.UpdateDisplay());
        }

        [Test]
        public void UpdateDisplay_EnoughScraps_ButtonIsInteractable()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);
            _controller.UpdateDisplay(75);
            Assert.IsTrue(_buttonGo.GetComponent<Button>().interactable);
        }

        [Test]
        public void UpdateDisplay_NotEnoughScraps_ButtonIsNotInteractable()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(50);
            _controller.UpdateDisplay(75);
            Assert.IsFalse(_buttonGo.GetComponent<Button>().interactable);
        }

        [Test]
        public void OnRepairButtonClicked_SpendsScraps()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);
            
            _controller.OnRepairButtonClicked();

            Assert.AreEqual(25, RunSessionManager.Scraps);
        }

        [Test]
        public void ButtonClick_InvokesRepairAndSpendsScraps()
        {
            RunSessionManager.Clear();
            RunSessionManager.GrantScraps(100);

            // Update display so the button becomes interactable again
            _controller.UpdateDisplay();

            // Invoke the button's onClick event
            var button = _buttonGo.GetComponent<Button>();
            if (button.interactable)
            {
                button.onClick.Invoke();
            }

            Assert.AreEqual(25, RunSessionManager.Scraps);
        }
    }
}
