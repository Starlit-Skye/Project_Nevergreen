using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace Nevergreen.Prototype
{
    /// <summary>
    /// Editor-only utility to build the CombatPrototype scene structure.
    /// Run from menu: Nevergreen/Setup Combat Scene.
    /// </summary>
    public static class CombatSceneBuilder
    {
#if UNITY_EDITOR
        [MenuItem("Nevergreen/Setup Combat Scene")]
        public static void BuildCombatScene()
        {
            // Cleanup existing root objects if we run multiple times in same scene
            System.Collections.Generic.List<GameObject> oldPlayerTeam = new System.Collections.Generic.List<GameObject>();
            System.Collections.Generic.List<GameObject> oldEnemyTeam = new System.Collections.Generic.List<GameObject>();

            var oldUICanvas = GameObject.Find("UICanvas");
            if (oldUICanvas) Object.DestroyImmediate(oldUICanvas);
            var oldWorldCanvas = GameObject.Find("WorldCanvas");
            if (oldWorldCanvas) Object.DestroyImmediate(oldWorldCanvas);
            var oldBattleSystem = GameObject.Find("BattleSystem");
            if (oldBattleSystem) Object.DestroyImmediate(oldBattleSystem);
            var oldEventSystem = GameObject.Find("EventSystem");
            if (oldEventSystem) Object.DestroyImmediate(oldEventSystem);
            var oldBootstrap = GameObject.Find("CombatSceneBootstrap");
            if (oldBootstrap)
            {
                var comp = oldBootstrap.GetComponent<CombatSceneBootstrap>();
                if (comp != null)
                {
                    if (comp.playerTeamPrefabs != null) oldPlayerTeam.AddRange(comp.playerTeamPrefabs);
                    if (comp.enemyTeamPrefabs != null) oldEnemyTeam.AddRange(comp.enemyTeamPrefabs);
                }
                Object.DestroyImmediate(oldBootstrap);
            }
            var oldGround = GameObject.Find("Ground");
            if (oldGround) Object.DestroyImmediate(oldGround);

            // --- Camera setup ---
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.transform.position = new Vector3(0, 1, -10);
                cam.backgroundColor = new Color(0.1f, 0.08f, 0.12f);
            }

            // --- BattleSystem ---
            var battleSystemGO = new GameObject("BattleSystem");
            var battleSystem = battleSystemGO.AddComponent<Combat.BattleSystem>();
            battleSystemGO.AddComponent<Combat.BattleMusicController>();

            // --- Combat Config asset ---
            var configPath = "Assets/Data/CombatConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<Data.CombatConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<Data.CombatConfig>();
                EnsureFolderExists("Assets/Data");
                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
            }

            // --- Audio Config asset ---
            var audioConfigPath = "Assets/Data/GlobalAudioConfig.asset";
            var audioConfig = AssetDatabase.LoadAssetAtPath<Data.AudioConfig>(audioConfigPath);
            if (audioConfig == null)
            {
                audioConfig = ScriptableObject.CreateInstance<Data.AudioConfig>();
                EnsureFolderExists("Assets/Data");
                AssetDatabase.CreateAsset(audioConfig, audioConfigPath);
                AssetDatabase.SaveAssets();
            }

            // --- AudioManager ---
            var audioManagerGO = GameObject.Find("AudioManager");
            if (audioManagerGO == null)
            {
                var prefabPath = "Assets/Prefabs/Managers/AudioManager.prefab";
                var amPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (amPrefab != null)
                {
                    audioManagerGO = (GameObject)PrefabUtility.InstantiatePrefab(amPrefab);
                    var am = audioManagerGO.GetComponent<Audio.AudioManager>();
                    if (am != null)
                    {
                        am.config = audioConfig;
                        EditorUtility.SetDirty(am);
                    }
                    Debug.Log($"[CombatSceneBuilder] Instantiated AudioManager from prefab: {prefabPath}");
                }
                else
                {
                    Debug.LogWarning($"[CombatSceneBuilder] AudioManager prefab not found at {prefabPath}. Falling back to manual creation.");
                    audioManagerGO = new GameObject("AudioManager");
                    var am = audioManagerGO.AddComponent<Audio.AudioManager>();
                    am.config = audioConfig;

                    // Fallback source creation
                    var bgmMain = audioManagerGO.AddComponent<AudioSource>();
                    bgmMain.playOnAwake = false; bgmMain.loop = true;
                    var bgmFade = audioManagerGO.AddComponent<AudioSource>();
                    bgmFade.playOnAwake = false; bgmFade.loop = true; bgmFade.volume = 0;
                    var sfx = audioManagerGO.AddComponent<AudioSource>();
                    sfx.playOnAwake = false;

                    var so = new SerializedObject(am);
                    so.FindProperty("_bgmSourceMain").objectReferenceValue = bgmMain;
                    so.FindProperty("_bgmSourceFade").objectReferenceValue = bgmFade;
                    so.FindProperty("_sfxSource").objectReferenceValue = sfx;
                    so.ApplyModifiedProperties();
                }
            }
            else
            {
                var am = audioManagerGO.GetComponent<Audio.AudioManager>();
                if (am != null) am.config = audioConfig;
            }

            // --- World Space Canvas for HP Bars ---
            var worldCanvasGO = new GameObject("WorldCanvas");
            var worldCanvas = worldCanvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvasGO.AddComponent<CanvasScaler>();
            worldCanvasGO.AddComponent<GraphicRaycaster>();

            var worldCanvasRT = worldCanvasGO.GetComponent<RectTransform>();
            worldCanvasRT.sizeDelta = new Vector2(20, 12);
            worldCanvasRT.position = Vector3.zero;
            worldCanvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // --- HP Bar Prefab ---
            var hpBarPrefab = CreateHPBarPrefab();

            // --- Event System ---
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // --- Screen Space UI Canvas ---
            var uiCanvasGO = new GameObject("UICanvas");
            var uiCanvas = uiCanvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 10;
            uiCanvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            uiCanvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            uiCanvasGO.AddComponent<GraphicRaycaster>();

            // --- Bottom Panel ---
            var bottomPanel = CreateUIPanel(uiCanvasGO.transform, "BottomPanel",
                new Vector2(0, 0), new Vector2(1, 0.25f),
                new Color(0.05f, 0.05f, 0.08f, 0.9f));

            // --- Skill Panel (left side of bottom) ---
            var skillPanel = CreateUIPanel(bottomPanel.transform, "SkillPanel",
                new Vector2(0, 0), new Vector2(0.55f, 1),
                new Color(0f, 0f, 0f, 0f));

            // Skill Buttons
            var skillButtons = new Button[4];
            var skillLabels = new TextMeshProUGUI[4];
            string[] defaultSkillNames = { "Skill 1", "Skill 2", "Skill 3", "Skill 4" };
            for (int i = 0; i < 4; i++)
            {
                var btn = CreateSkillButton(skillPanel.transform, defaultSkillNames[i], i);
                skillButtons[i] = btn.GetComponent<Button>();
                skillLabels[i] = btn.GetComponentInChildren<TextMeshProUGUI>();
                btn.SetActive(false);
            }

            // Move Button
            var moveBtn = CreateSkillButton(skillPanel.transform, "Move", 4);
            moveBtn.SetActive(false);

            // Pass Button
            var passBtn = CreateSkillButton(skillPanel.transform, "Pass", 5);
            passBtn.SetActive(false);

            // --- Stats Panel (right side of bottom) ---
            var statsPanel = CreateUIPanel(bottomPanel.transform, "StatsPanel",
                new Vector2(0.55f, 0), new Vector2(1, 1),
                new Color(0.08f, 0.06f, 0.1f, 0.9f));

            var statsText = CreateText(statsPanel.transform, "StatsText",
                "Hover over a character to see stats", 16,
                TextAlignmentOptions.TopLeft, new Vector2(10, -10));

            // --- Top HUD ---
            var topPanel = CreateUIPanel(uiCanvasGO.transform, "TopPanel",
                new Vector2(0, 0.92f), new Vector2(1, 1),
                new Color(0.05f, 0.05f, 0.08f, 0.8f));

            var roundText = CreateText(topPanel.transform, "RoundText", "Round 1", 24,
                TextAlignmentOptions.Center);
            var turnText = CreateText(topPanel.transform, "TurnText", "", 18,
                TextAlignmentOptions.Center, new Vector2(0, -30));

            // --- Battle Log (mid-left) ---
            var logPanel = CreateUIPanel(uiCanvasGO.transform, "LogPanel",
                new Vector2(0, 0.25f), new Vector2(0.35f, 0.55f),
                new Color(0.02f, 0.02f, 0.04f, 0.7f));

            var logText = CreateText(logPanel.transform, "BattleLogText", "", 14,
                TextAlignmentOptions.BottomLeft, new Vector2(10, 5));

            // --- Battle End Panel (center, hidden) ---
            var endPanel = CreateUIPanel(uiCanvasGO.transform, "BattleEndPanel",
                new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.65f),
                new Color(0.02f, 0.02f, 0.05f, 0.95f));

            var endText = CreateText(endPanel.transform, "BattleEndText", "VICTORY!", 48,
                TextAlignmentOptions.Center);
            endPanel.SetActive(false);

            // --- CombatUI component ---
            var combatUI = uiCanvasGO.AddComponent<CombatUI>();
            combatUI.skillPanel = skillPanel;
            combatUI.statsPanel = statsPanel;
            combatUI.roundText = roundText.GetComponent<TextMeshProUGUI>();
            combatUI.turnText = turnText.GetComponent<TextMeshProUGUI>();
            combatUI.battleLogText = logText.GetComponent<TextMeshProUGUI>();
            combatUI.statsDisplayText = statsText.GetComponent<TextMeshProUGUI>();
            combatUI.battleEndPanel = endPanel;
            combatUI.battleEndText = endText.GetComponent<TextMeshProUGUI>();
            combatUI.skillButtons = skillButtons;
            combatUI.skillButtonLabels = skillLabels;
            combatUI.moveButton = moveBtn.GetComponent<Button>();
            combatUI.passButton = passBtn.GetComponent<Button>();
            combatUI.hpBarPrefab = hpBarPrefab;
            combatUI.worldSpaceCanvas = worldCanvas;

            // --- CombatSceneBootstrap ---
            var bootstrapGO = new GameObject("CombatSceneBootstrap");
            var bootstrap = bootstrapGO.AddComponent<CombatSceneBootstrap>();
            bootstrap.battleSystem = battleSystem;
            bootstrap.combatUI = combatUI;

            // Restore or insert default prefabs
            if (oldPlayerTeam.Count > 0 || oldEnemyTeam.Count > 0)
            {
                bootstrap.playerTeamPrefabs = oldPlayerTeam;
                bootstrap.enemyTeamPrefabs = oldEnemyTeam;
            }
            else
            {
                // Default test setup
                var knight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Knight.prefab");
                var maid = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Maid.prefab");
                var ceci = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Ceci.prefab");
                var golem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/StoneGolem.prefab");
                var mascot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/TwistedMascot.prefab");

                if (knight) bootstrap.playerTeamPrefabs.Add(knight);
                if (maid) bootstrap.playerTeamPrefabs.Add(maid);
                if (ceci) bootstrap.playerTeamPrefabs.Add(ceci);

                if (golem) bootstrap.enemyTeamPrefabs.Add(golem);
                if (mascot) bootstrap.enemyTeamPrefabs.Add(mascot);
                if (golem) bootstrap.enemyTeamPrefabs.Add(golem);
            }

            // --- Ground placeholder ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -1.5f, 0);
            ground.transform.localScale = new Vector3(30, 1, 1);
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.material.color = new Color(0.15f, 0.12f, 0.18f);

            // Save
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[CombatSceneBuilder] Combat scene setup complete!");
        }

        private static GameObject CreateHPBarPrefab()
        {
            var prefabPath = "Assets/Prefabs/UI/HPBar.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            // Build HP bar structure
            var barRoot = new GameObject("HPBar");

            var canvasOnBar = barRoot.AddComponent<Canvas>();
            canvasOnBar.renderMode = RenderMode.WorldSpace;
            barRoot.AddComponent<GraphicRaycaster>();
            barRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 30);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(barRoot.transform, false);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barRoot.transform, false);
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0.02f, 0.1f);
            fillRT.anchorMax = new Vector2(0.98f, 0.9f);
            fillRT.sizeDelta = Vector2.zero;

            // Name text
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(barRoot.transform, false);
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = "Name";
            nameText.fontSize = 12;
            nameText.alignment = TextAlignmentOptions.Top;
            nameText.color = Color.white;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1);
            nameRT.anchorMax = new Vector2(1, 1.8f);
            nameRT.sizeDelta = Vector2.zero;

            // HP text
            var hpGO = new GameObject("HPText");
            hpGO.transform.SetParent(barRoot.transform, false);
            var hpText = hpGO.AddComponent<TextMeshProUGUI>();
            hpText.text = "50/50";
            hpText.fontSize = 10;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;
            var hpRT = hpGO.GetComponent<RectTransform>();
            hpRT.anchorMin = Vector2.zero;
            hpRT.anchorMax = Vector2.one;
            hpRT.sizeDelta = Vector2.zero;

            // Status Icon Container
            var statusContainerGO = new GameObject("StatusIconContainer");
            statusContainerGO.transform.SetParent(barRoot.transform, false);
            var statusRT = statusContainerGO.AddComponent<RectTransform>();
            // Position above NameText
            statusRT.anchorMin = new Vector2(0, 1.8f);
            statusRT.anchorMax = new Vector2(1, 2.6f);
            statusRT.sizeDelta = Vector2.zero;
            var hlg = statusContainerGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.LowerCenter;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 2f;

            // Create default StatusIcon prefab if it doesn't exist
            string statusIconPrefabPath = "Assets/Prefabs/UI/StatusIcon.prefab";
            var statusIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(statusIconPrefabPath);
            if (statusIconPrefab == null)
            {
                var iconRoot = new GameObject("StatusIcon");
                var iconRT = iconRoot.AddComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(24, 24);
                
                var iconImg = iconRoot.AddComponent<Image>();
                iconImg.color = Color.white;

                var countTextGO = new GameObject("CountText");
                countTextGO.transform.SetParent(iconRoot.transform, false);
                var countText = countTextGO.AddComponent<TextMeshProUGUI>();
                countText.text = "";
                countText.fontSize = 12;
                countText.alignment = TextAlignmentOptions.BottomRight;
                countText.color = Color.white;
                countText.outlineWidth = 0.2f;
                countText.outlineColor = Color.black;
                var countRT = countTextGO.GetComponent<RectTransform>();
                countRT.anchorMin = Vector2.zero;
                countRT.anchorMax = Vector2.one;
                countRT.sizeDelta = Vector2.zero;
                countRT.anchoredPosition = new Vector2(2, -2);

                EnsureFolderExists("Assets/Prefabs/UI");
                statusIconPrefab = PrefabUtility.SaveAsPrefabAsset(iconRoot, statusIconPrefabPath);
                Object.DestroyImmediate(iconRoot);
            }

            // Add HPBar component
            var hpBar = barRoot.AddComponent<HPBar>();
            hpBar.fillImage = fillImage;
            hpBar.nameText = nameText;
            hpBar.hpText = hpText;
            hpBar.statusIconContainer = statusRT;
            hpBar.statusIconPrefab = statusIconPrefab;

            // Save as prefab
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/Prefabs/UI");
            var prefab = PrefabUtility.SaveAsPrefabAsset(barRoot, prefabPath);
            Object.DestroyImmediate(barRoot);

            return prefab;
        }

        private static GameObject CreateUIPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            int fontSize, TextAlignmentOptions alignment, Vector2 offset = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = offset;
            return go;
        }

        private static GameObject CreateSkillButton(Transform parent, string label, int index)
        {
            var go = new GameObject($"SkillButton_{index}");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.12f, 0.22f, 1f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.22f);
            colors.highlightedColor = new Color(0.25f, 0.2f, 0.35f);
            colors.pressedColor = new Color(0.35f, 0.25f, 0.45f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            float btnWidth = 0.22f;
            float gap = 0.01f;
            float startX = 0.02f;
            float x = startX + index * (btnWidth + gap);
            rt.anchorMin = new Vector2(x, 0.15f);
            rt.anchorMax = new Vector2(x + btnWidth, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.sizeDelta = Vector2.zero;

            return go;
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureFolderExists(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
#endif
    }
}
