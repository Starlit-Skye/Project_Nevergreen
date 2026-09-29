using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Room effect strategy for Elite combat encounters.
    /// Overrides the encounter tier to EnemyEncounterTier.Elite and handles victory room completion and trinket rewards.
    /// </summary>
    [Serializable]
    public class EliteEncounterRoomEffectStrategy : CombatRoomEffectStrategy
    {
        [Tooltip("Prefab for the Elite Reward UI. Must contain EliteRewardUIController.")]
        [SerializeField] private GameObject eliteRewardUiPrefab;

        public override EnemyEncounterTier? OverrideEncounterTier => EnemyEncounterTier.Elite;

        public override void ExecuteRoomEffect()
        {
            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            var rolledTrinket = RollTrinketReward();

            if (eliteRewardUiPrefab == null)
            {
                if (rolledTrinket != null)
                {
                    Debug.Log($"[EliteEncounter] Elite battle victory rewarded trinket: {rolledTrinket.displayName} (Fallback Log)");
                }
                base.ExecuteRoomEffect();
                return;
            }

            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas canvas = null;

            foreach (var c in canvases)
            {
                if (c.name == "UICanvas" && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                {
                    canvas = c;
                    break;
                }
            }

            if (canvas == null)
            {
                foreach (var c in canvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        canvas = c;
                        break;
                    }
                }
            }

            if (canvas == null)
            {
                if (rolledTrinket != null) Debug.Log($"[EliteEncounter] Elite battle victory rewarded trinket: {rolledTrinket.displayName} (Fallback Log)");
                base.ExecuteRoomEffect();
                return;
            }

            var uiInstance = UnityEngine.Object.Instantiate(eliteRewardUiPrefab, canvas.transform);
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            
            var controller = uiInstance.GetComponent<Nevergreen.UI.EliteRewardUIController>();
            if (controller != null)
            {
                controller.Initialize(rolledTrinket, RunSessionManager.CurrentParty);
            }
            else
            {
                if (rolledTrinket != null) Debug.Log($"[EliteEncounter] Elite battle victory rewarded trinket: {rolledTrinket.displayName} (Fallback Log)");
                UnityEngine.Object.Destroy(uiInstance);
                base.ExecuteRoomEffect();
            }
        }

        private TrinketData RollTrinketReward()
        {
            var config = GameDatabase.Instance != null ? GameDatabase.Instance.CombatConfig : null;
            var database = GameDatabase.Instance != null ? GameDatabase.Instance.TrinketDatabase : null;

            float commonWeight = config != null ? config.eliteTrinketCommonWeight : 50f;
            float uncommonWeight = config != null ? config.eliteTrinketUncommonWeight : 35f;
            float rareWeight = config != null ? config.eliteTrinketRareWeight : 15f;

            float totalWeight = commonWeight + uncommonWeight + rareWeight;
            if (totalWeight <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            IReadOnlyList<TrinketData> selectedPool = null;

            if (roll < commonWeight)
            {
                selectedPool = database?.CommonTrinkets;
            }
            else if (roll < commonWeight + uncommonWeight)
            {
                selectedPool = database?.UncommonTrinkets;
            }
            else
            {
                selectedPool = database?.RareTrinkets;
            }

            if (selectedPool != null && selectedPool.Count > 0)
            {
                var validTrinkets = selectedPool.Where(t => t != null).ToList();
                if (validTrinkets.Count > 0)
                {
                    return validTrinkets[UnityEngine.Random.Range(0, validTrinkets.Count)];
                }
            }

            return null;
        }
    }
}
