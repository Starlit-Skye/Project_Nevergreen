using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class GenericEventRoomEffectStrategyTests
    {
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null && !UnityEditor.EditorUtility.IsPersistent(obj))
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateMockGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        [Test]
        public void GetRandomPrefab_WhenPrefabsListIsEmptyOrNull_ReturnsNull()
        {
            var strategy = new GenericEventRoomEffectStrategy();
            strategy.eventUiPrefabs = null;
            Assert.IsNull(strategy.GetRandomPrefab());

            strategy.eventUiPrefabs = new List<GameObject>();
            Assert.IsNull(strategy.GetRandomPrefab());
        }

        [Test]
        public void GetRandomPrefab_WhenPrefabsListHasSingleItem_ReturnsThatItem()
        {
            var mockPrefab = CreateMockGameObject("TestEventUI_Single");
            var strategy = new GenericEventRoomEffectStrategy();
            strategy.eventUiPrefabs.Add(mockPrefab);

            var chosen = strategy.GetRandomPrefab();

            Assert.AreSame(mockPrefab, chosen);
        }

        [Test]
        public void GetRandomPrefab_WhenPrefabsListHasMultipleItems_ReturnsOneOfItems()
        {
            var prefab1 = CreateMockGameObject("TestEventUI_1");
            var prefab2 = CreateMockGameObject("TestEventUI_2");
            var strategy = new GenericEventRoomEffectStrategy();
            strategy.eventUiPrefabs.Add(prefab1);
            strategy.eventUiPrefabs.Add(prefab2);

            var chosen = strategy.GetRandomPrefab();

            Assert.IsTrue(chosen == prefab1 || chosen == prefab2);
        }

        [Test]
        public void ExecuteRoomEffect_WhenNoPrefabsAssigned_LogsErrorAndDoesNotThrow()
        {
            var strategy = new GenericEventRoomEffectStrategy();
            strategy.eventUiPrefabs.Clear();

            LogAssert.Expect(LogType.Error, "[GenericEventRoomEffectStrategy] No eventUiPrefabs assigned!");
            Assert.DoesNotThrow(() => strategy.ExecuteRoomEffect());
        }

        [Test]
        public void ExecuteRoomEffect_WhenValidPrefabAndCanvasExist_InstantiatesUnderCanvas()
        {
            var canvasGo = new GameObject("UICanvas", typeof(Canvas), typeof(RectTransform));
            _createdObjects.Add(canvasGo);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var prefab = CreateMockGameObject("SampleEventPanel");
            prefab.AddComponent<RectTransform>();

            var strategy = new GenericEventRoomEffectStrategy();
            strategy.eventUiPrefabs.Add(prefab);

            Assert.DoesNotThrow(() => strategy.ExecuteRoomEffect());

            // Check that a child instance was created under the canvas
            Assert.AreEqual(1, canvasGo.transform.childCount);
            var child = canvasGo.transform.GetChild(0).gameObject;
            _createdObjects.Add(child);
            Assert.IsTrue(child.name.StartsWith("SampleEventPanel"));
        }
    }
}
