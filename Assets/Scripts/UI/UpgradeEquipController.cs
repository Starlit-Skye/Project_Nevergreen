using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Upgrade/Reforge Equipment Room UI.
    /// Allows the player to place an equipped trinket into the Test_Trinket_Dropper and reforge it
    /// into a trinket of the next rarity tier (or another Rare if already Rare).
    /// </summary>
    public class UpgradeEquipController : MonoBehaviour, ITrinketEquipContainerUI
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject reforgePanel;

        [Header("Buttons")]
        [SerializeField] private Button reforgeButton;
        [SerializeField] private Button leaveButton;

        [Header("Containers")]
        [Tooltip("The Test_Trinket_Dropper container holding the player's slotted trinket for reforging.")]
        [SerializeField] private Transform testTrinketDroppedContainer;

        [Header("Prefabs & Cache")]
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        private List<PartyMemberInfo> _party;
        private bool _hasReforged;

        public bool HasReforged => _hasReforged;

        public void RefreshTrinketUI()
        {
            RefreshPanels();
        }

        public void OnUnassignedTrinketChanged(TrinketData oldTrinket, TrinketData newTrinket)
        {
            // Upgrade room does not track an unassigned rolled reward trinket
        }

        void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }
        /// <summary>
        /// Initializes the upgrade room with party member info and hides the reforge panel.
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party;
            _hasReforged = false;

            if (reforgePanel != null)
            {
                reforgePanel.SetActive(false);
            }

            RefreshPanels();
        }

        public void RefreshPanels()
        {
            if (trinketEquipPanels == null || trinketEquipPanels.Count == 0)
            {
                trinketEquipPanels = TrinketEquipPanelHelper.FindEquipPanels(this);
            }

            TrinketEquipPanelHelper.RefreshPanels(trinketEquipPanels, _party, trinketUIItemPrefab);
        }

        private void OnEnable()
        {
            if (reforgeButton != null)
            {
                reforgeButton.onClick.AddListener(OnReforgeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (reforgeButton != null)
            {
                reforgeButton.onClick.RemoveListener(OnReforgeClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        /// <summary>
        /// Handles clicking the Reforge button. If already reforged, completes the room.
        /// Otherwise checks if a trinket is slotted in the dropper and upgrades it.
        /// </summary>
        public void OnReforgeClicked()
        {
            if (_hasReforged)
            {
                CompleteRoom();
                return;
            }

            if (testTrinketDroppedContainer == null)
            {
                return;
            }

            var uiItem = testTrinketDroppedContainer.GetComponentInChildren<TrinketUIItem>();
            if (uiItem == null || uiItem.TrinketData == null)
            {
                return;
            }

            var db = GameDatabase.Instance != null ? GameDatabase.Instance.TrinketDatabase : null;
            if (db == null)
            {
                Debug.LogWarning("[UpgradeEquipController] TrinketDatabase not found!");
                return;
            }

            TrinketData currentTrinket = uiItem.TrinketData;
            List<TrinketData> targetPool = null;

            if (ContainsTrinket(db.CommonTrinkets, currentTrinket))
            {
                targetPool = db.UncommonTrinkets != null ? new List<TrinketData>(db.UncommonTrinkets) : null;
            }
            else if (ContainsTrinket(db.UncommonTrinkets, currentTrinket))
            {
                targetPool = db.RareTrinkets != null ? new List<TrinketData>(db.RareTrinkets) : null;
            }
            else if (ContainsTrinket(db.RareTrinkets, currentTrinket))
            {
                if (db.RareTrinkets != null)
                {
                    targetPool = new List<TrinketData>();
                    for (int i = 0; i < db.RareTrinkets.Count; i++)
                    {
                        var t = db.RareTrinkets[i];
                        if (t != null && t != currentTrinket)
                        {
                            targetPool.Add(t);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[UpgradeEquipController] Trinket '{currentTrinket.displayName}' is not found in Common, Uncommon, or Rare database pools!");
                return;
            }

            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning("[UpgradeEquipController] No upgrade trinket available in the target pool!");
                return;
            }

            int randomIndex = Random.Range(0, targetPool.Count);
            TrinketData newTrinket = targetPool[randomIndex];

            uiItem.Initialize(newTrinket);
            var tooltip = uiItem.GetComponent<TrinketTooltipTrigger>();
            if (tooltip != null)
            {
                tooltip.SetTrinket(newTrinket);
            }

            _hasReforged = true;

            if (reforgeButton != null)
            {
                var btnText = reforgeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "Leave";
                }
            }
        }

        /// <summary>
        /// Handles clicking the Leave button to exit the room.
        /// </summary>
        public void OnLeaveClicked()
        {
            CompleteRoom();
        }

        /// <summary>
        /// Completes the room flow by deactivating this room and opening the room selection map.
        /// </summary>
        public void CompleteRoom()
        {
            gameObject.SetActive(false);

            var combatUI = FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogError("[UpgradeEquipController] CombatUI not found!");
            }
        }

        private static bool ContainsTrinket(IReadOnlyList<TrinketData> list, TrinketData target)
        {
            if (list == null || target == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == target) return true;
            }
            return false;
        }
    }
}
