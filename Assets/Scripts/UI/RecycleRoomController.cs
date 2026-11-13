using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Recycle Room UI.
    /// Allows the player to open the recycle selection panel, drag equipped trinkets into the
    /// dropper, and recycle them for Scraps based on rarity.
    /// </summary>
    public class RecycleRoomController : MonoBehaviour, ITrinketEquipContainerUI
    {
        [Header("Buttons")]
        [SerializeField] private Button recycleButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button leaveButton;

        [Header("Panels & Text")]
        [SerializeField] private GameObject recycleSelectionPanel;
        [SerializeField] private TextMeshProUGUI scrapsRecvText;

        [Header("Trinket UI")]
        [Tooltip("The Test_Trinket_Dropper container where the player drops trinkets to recycle.")]
        [SerializeField] private Transform testTrinketDroppedContainer;
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        [Header("Scrap Configuration")]
        [Min(0)]
        [SerializeField] private int commonScraps = 10;
        [Min(0)]
        [SerializeField] private int uncommonScraps = 20;
        [Min(0)]
        [SerializeField] private int rareScraps = 40;

        private List<PartyMemberInfo> _party;

        public int CommonScraps
        {
            get => commonScraps;
            set => commonScraps = Mathf.Max(0, value);
        }

        public int UncommonScraps
        {
            get => uncommonScraps;
            set => uncommonScraps = Mathf.Max(0, value);
        }

        public int RareScraps
        {
            get => rareScraps;
            set => rareScraps = Mathf.Max(0, value);
        }

        public void RefreshTrinketUI()
        {
            RefreshPanels();
            EvaluateConfirmButton();
        }

        public void OnUnassignedTrinketChanged(TrinketData oldTrinket, TrinketData newTrinket)
        {
            EvaluateConfirmButton();
        }

        private void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }

        /// <summary>
        /// Initializes the recycle room with the active party, hides the selection panel,
        /// clears any stale items from the dropper, and disables Confirm.
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;

            if (recycleSelectionPanel != null)
            {
                recycleSelectionPanel.SetActive(false);
            }

            if (recycleButton != null)
            {
                recycleButton.gameObject.SetActive(true);
            }

            ClearDropperContainer();
            RefreshPanels();
            EvaluateConfirmButton();
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
            if (recycleButton != null)
            {
                recycleButton.onClick.AddListener(OnRecycleClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (recycleButton != null)
            {
                recycleButton.onClick.RemoveListener(OnRecycleClicked);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        private void Update()
        {
            EvaluateConfirmButton();
        }

        /// <summary>
        /// Opens the recycle selection panel without hiding the Recycle button or generating a trinket in the dropper.
        /// </summary>
        public void OnRecycleClicked()
        {
            if (recycleSelectionPanel != null)
            {
                recycleSelectionPanel.SetActive(true);
            }

            RefreshPanels();
            EvaluateConfirmButton();
        }

        /// <summary>
        /// Updates the interactable state of the Confirm button based on whether the dropper contains a valid trinket.
        /// </summary>
        public void EvaluateConfirmButton()
        {
            if (confirmButton == null) return;

            bool hasValidTrinket = false;
            if (testTrinketDroppedContainer != null)
            {
                for (int i = 0; i < testTrinketDroppedContainer.childCount; i++)
                {
                    var child = testTrinketDroppedContainer.GetChild(i);
                    if (child == null || !child.gameObject.activeInHierarchy && !child.gameObject.activeSelf) continue;

                    var uiItem = child.GetComponent<TrinketUIItem>();
                    if (uiItem != null && uiItem.TrinketData != null)
                    {
                        hasValidTrinket = true;
                        break;
                    }
                }
            }

            confirmButton.interactable = hasValidTrinket;
        }

        /// <summary>
        /// Consumes the trinket(s) in the dropper and grants Scraps according to rarity.
        /// Keeps the room open so the player can recycle additional trinkets.
        /// </summary>
        public void OnConfirmClicked()
        {
            if (testTrinketDroppedContainer == null) return;

            var db = GameDatabase.Instance != null ? GameDatabase.Instance.TrinketDatabase : null;
            if (db == null)
            {
                Debug.LogWarning("[RecycleRoomController] TrinketDatabase not found!");
                return;
            }

            int totalScrapsToGrant = 0;
            var itemsToConsume = new List<GameObject>();

            for (int i = 0; i < testTrinketDroppedContainer.childCount; i++)
            {
                var child = testTrinketDroppedContainer.GetChild(i);
                if (child == null) continue;

                var uiItem = child.GetComponent<TrinketUIItem>();
                if (uiItem == null || uiItem.TrinketData == null) continue;

                TrinketData trinket = uiItem.TrinketData;

                if (ContainsTrinket(db.CommonTrinkets, trinket))
                {
                    totalScrapsToGrant += commonScraps;
                    itemsToConsume.Add(uiItem.gameObject);
                }
                else if (ContainsTrinket(db.UncommonTrinkets, trinket))
                {
                    totalScrapsToGrant += uncommonScraps;
                    itemsToConsume.Add(uiItem.gameObject);
                }
                else if (ContainsTrinket(db.RareTrinkets, trinket))
                {
                    totalScrapsToGrant += rareScraps;
                    itemsToConsume.Add(uiItem.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[RecycleRoomController] Trinket '{trinket.displayName}' is not found in Common, Uncommon, or Rare database pools!");
                }
            }

            for (int i = 0; i < itemsToConsume.Count; i++)
            {
                DestroyObjectSafe(itemsToConsume[i]);
            }

            if (totalScrapsToGrant > 0)
            {
                RunSessionManager.GrantScraps(totalScrapsToGrant);
            }

            if (itemsToConsume.Count > 0 && scrapsRecvText != null)
            {
                scrapsRecvText.text = $"Received {totalScrapsToGrant} Scraps";
            }

            RefreshPanels();
            EvaluateConfirmButton();
        }

        /// <summary>
        /// Handles clicking the Leave button to complete the room.
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
                Debug.LogError("[RecycleRoomController] CombatUI not found!");
            }
        }

        private void ClearDropperContainer()
        {
            if (testTrinketDroppedContainer == null) return;

            for (int i = testTrinketDroppedContainer.childCount - 1; i >= 0; i--)
            {
                var child = testTrinketDroppedContainer.GetChild(i);
                if (child != null)
                {
                    DestroyObjectSafe(child.gameObject);
                }
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

        private static void DestroyObjectSafe(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}

