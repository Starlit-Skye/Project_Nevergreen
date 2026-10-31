using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    /// <summary>
    /// UI Controller for the Armament Builder room.
    /// Spends Scraps to roll a random Common or Uncommon trinket equipment, allowing the player to equip it.
    /// </summary>
    public class ArmamentBuilderRoomController : MonoBehaviour, ITrinketEquipContainerUI
    {
        [Header("UI Elements")]
        [SerializeField] private Button recvEquipmentButton;
        [SerializeField] private Button leaveButton1;
        [SerializeField] private Button leaveButton2;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private TextMeshProUGUI rewardText;

        [Header("Configuration")]
        [Tooltip("Scraps cost required to receive random equipment.")]
        [SerializeField, Min(0)] private int scrapsCost = 20;

        [Tooltip("Weight for dropping a Common tier trinket. Set to 0 to disable Common trinkets.")]
        [SerializeField, Min(0f)] private float commonTrinketWeight = 70f;

        [Tooltip("Weight for dropping an Uncommon tier trinket. Set to 0 to disable Uncommon trinkets.")]
        [SerializeField, Min(0f)] private float uncommonTrinketWeight = 30f;

        [Header("Trinket UI")]
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private Transform testTrinketDroppedContainer;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        private TrinketData _rolledTrinket;
        private List<PartyMemberInfo> _party;
        private bool _hasReceived;

        public int ScrapsCost
        {
            get => scrapsCost;
            set => scrapsCost = Mathf.Max(0, value);
        }

        public float CommonTrinketWeight
        {
            get => commonTrinketWeight;
            set => commonTrinketWeight = Mathf.Max(0f, value);
        }

        public float UncommonTrinketWeight
        {
            get => uncommonTrinketWeight;
            set => uncommonTrinketWeight = Mathf.Max(0f, value);
        }

        public TrinketData RolledTrinket => _rolledTrinket;
        public bool HasReceived => _hasReceived;

        public void RefreshTrinketUI()
        {
            RefreshPanels();
        }

        public void OnUnassignedTrinketChanged(TrinketData oldTrinket, TrinketData newTrinket)
        {
            _rolledTrinket = newTrinket;
        }

        void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }
        /// <summary>
        /// Initializes the UI state and party equipment panels.
        /// </summary>
        public void Initialize(List<PartyMemberInfo> party = null)
        {
            _party = party ?? RunSessionManager.CurrentParty;
            _rolledTrinket = null;
            _hasReceived = false;

            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }

            if (recvEquipmentButton != null)
            {
                recvEquipmentButton.gameObject.SetActive(true);

                var btnText = recvEquipmentButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"Spend {scrapsCost} Scraps, receive a random equipment.";
                }
            }

            RefreshPanels();
        }

        public void RefreshPanels()
        {
            if (trinketEquipPanels == null || trinketEquipPanels.Count == 0)
            {
                trinketEquipPanels = new List<GameObject>();
                var nameTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var nt in nameTexts)
                {
                    if (nt.name == "TrinketEquipMarionetteName")
                    {
                        trinketEquipPanels.Add(nt.transform.parent.gameObject);
                    }
                }
            }

            if (_party == null) return;

            for (int i = 0; i < trinketEquipPanels.Count; i++)
            {
                GameObject panel = trinketEquipPanels[i];
                if (panel == null) continue;

                if (i < _party.Count)
                {
                    panel.SetActive(true);
                    PartyMemberInfo member = _party[i];

                    // Set Marionette Name
                    var nameText = panel.transform.Find("TrinketEquipMarionetteName")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = member.character != null ? member.character.displayName : "Marionette";
                    }

                    // Setup Trinket 1
                    var trinket1 = panel.transform.Find("Trinket1");
                    if (trinket1 != null)
                    {
                        SetupTrinketSlot(trinket1, member, 0);
                    }

                    // Setup Trinket 2
                    var trinket2 = panel.transform.Find("Trinket2");
                    if (trinket2 != null)
                    {
                        SetupTrinketSlot(trinket2, member, 1);
                    }
                }
                else
                {
                    panel.SetActive(false);
                }
            }
        }

        private void SetupTrinketSlot(Transform container, PartyMemberInfo member, int slotIndex)
        {
            var dropHandler = container.GetComponent<TrinketSlotDropHandler>();
            if (dropHandler == null) dropHandler = container.gameObject.AddComponent<TrinketSlotDropHandler>();

            dropHandler.TargetMember = member;
            dropHandler.TargetSlotIndex = slotIndex;

            // Clear existing UI items
            foreach (Transform child in container)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            if (member.equippedTrinkets != null && slotIndex < member.equippedTrinkets.Count)
            {
                var trinket = member.equippedTrinkets[slotIndex];
                if (trinket == null) return;

                if (trinketUIItemPrefab != null)
                {
                    GameObject item = Instantiate(trinketUIItemPrefab, container);

                    var tooltipTrigger = item.GetComponent<TrinketTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TrinketTooltipTrigger>();
                    tooltipTrigger.SetTrinket(trinket);

                    var uiItem = item.GetComponent<TrinketUIItem>();
                    if (uiItem == null) uiItem = item.AddComponent<TrinketUIItem>();
                    uiItem.Initialize(trinket, member, slotIndex);
                }
            }
        }

        private void OnEnable()
        {
            if (recvEquipmentButton != null)
            {
                recvEquipmentButton.onClick.AddListener(OnRecvEquipmentClicked);
            }

            if (leaveButton1 != null)
            {
                leaveButton1.onClick.AddListener(OnLeaveClicked);
            }

            if (leaveButton2 != null)
            {
                leaveButton2.onClick.AddListener(OnLeaveClicked);
            }
        }

        private void OnDisable()
        {
            if (recvEquipmentButton != null)
            {
                recvEquipmentButton.onClick.RemoveListener(OnRecvEquipmentClicked);
            }

            if (leaveButton1 != null)
            {
                leaveButton1.onClick.RemoveListener(OnLeaveClicked);
            }

            if (leaveButton2 != null)
            {
                leaveButton2.onClick.RemoveListener(OnLeaveClicked);
            }
        }

        private void OnRecvEquipmentClicked()
        {
            if (_hasReceived) return;

            var rolled = RollTrinketReward();
            if (rolled == null)
            {
                Debug.LogWarning("[ArmamentBuilderRoomController] No trinket available to reward! Scraps not spent.");
                return;
            }

            if (!RunSessionManager.TrySpendScraps(scrapsCost))
            {
                return;
            }

            _rolledTrinket = rolled;
            _hasReceived = true;

            if (rewardText != null)
            {
                rewardText.text = $"You received {_rolledTrinket.displayName}!";
            }

            if (trinketUIItemPrefab != null && testTrinketDroppedContainer != null)
            {
                foreach (Transform child in testTrinketDroppedContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }

                var item = Instantiate(trinketUIItemPrefab, testTrinketDroppedContainer);
                var uiItem = item.GetComponent<TrinketUIItem>();
                if (uiItem != null)
                {
                    uiItem.Initialize(_rolledTrinket);
                }
            }

            if (recvEquipmentButton != null)
            {
                recvEquipmentButton.gameObject.SetActive(false);
            }

            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
            }
        }

        private void OnLeaveClicked()
        {
            CompleteRoom();
        }

        private void CompleteRoom()
        {
            gameObject.SetActive(false);

            CombatUI combatUI = UnityEngine.Object.FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogError("[ArmamentBuilderRoomController] CombatUI not found!");
            }
        }

        /// <summary>
        /// Rolls a random trinket based on the configured tier weights.
        /// Returns null if the rolled tier list is empty, or if TrinketDatabase is unavailable.
        /// </summary>
        public TrinketData RollTrinketReward(System.Random rng = null)
        {
            var db = GameDatabase.Instance;
            if (db == null || db.TrinketDatabase == null) return null;

            if (rng == null) rng = new System.Random();

            float totalWeight = commonTrinketWeight + uncommonTrinketWeight;
            if (totalWeight <= 0f) return null;

            double roll = rng.NextDouble() * totalWeight;
            bool isCommon = roll < commonTrinketWeight;

            var targetList = isCommon ? db.TrinketDatabase.CommonTrinkets : db.TrinketDatabase.UncommonTrinkets;

            if (targetList == null || targetList.Count == 0)
            {
                return null;
            }

            int index = rng.Next(targetList.Count);
            return targetList[index];
        }
    }
}
