using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    /// <summary>
    /// Controller for the Treasure Room UI.
    /// Handles rolling for Scraps and Parts, and the room completion flow.
    /// </summary>
    public class TreasureUIController : MonoBehaviour, ITrinketEquipContainerUI
    {
        public void RefreshTrinketUI()
        {
            RefreshPanels();
        }

        public void OnUnassignedTrinketChanged(TrinketData oldTrinket, TrinketData newTrinket)
        {
            _rolledTrinket = newTrinket;
        }
        [Header("UI Elements")]
        [SerializeField] private Button openTreasureButton;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Button closeButton;

        [Header("Trinket UI")]
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private Transform testTrinketDroppedContainer;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        private int _minScraps;
        private int _maxScraps;
        private int _minParts;
        private int _maxParts;

        private TrinketData _rolledTrinket;

        private int _rolledScraps;
        private int _rolledParts;

        private List<PartyMemberInfo> _party;

        /// <summary>
        /// Initializes the UI with the reward ranges and an optional trinket reward.
        /// </summary>
        public void Initialize(int minScraps, int maxScraps, int minParts, int maxParts, TrinketData rolledTrinket = null, List<PartyMemberInfo> party = null)
        {
            _minScraps = minScraps;
            _maxScraps = maxScraps;
            _minParts = minParts;
            _maxParts = maxParts;
            _rolledTrinket = rolledTrinket;
            _party = party ?? RunSessionManager.CurrentParty;

            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }

            if (openTreasureButton != null)
            {
                openTreasureButton.gameObject.SetActive(true);
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
            if (openTreasureButton != null)
            {
                openTreasureButton.onClick.AddListener(OnOpenTreasureClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (openTreasureButton != null)
            {
                openTreasureButton.onClick.RemoveListener(OnOpenTreasureClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnOpenTreasureClicked()
        {
            var rng = new System.Random();
            _rolledScraps = rng.Next(_minScraps, _maxScraps + 1);
            _rolledParts = rng.Next(_minParts, _maxParts + 1);

            if (rewardText != null)
            {
                if (_rolledTrinket != null)
                {
                    rewardText.text = $"You found {_rolledScraps} Scraps, {_rolledParts} Parts, and {_rolledTrinket.displayName}!";
                    
                    if (trinketUIItemPrefab != null && testTrinketDroppedContainer != null)
                    {
                        // Clear old drops
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
                }
                else
                {
                    rewardText.text = $"You found {_rolledScraps} Scraps and {_rolledParts} Parts!";
                }
            }

            if (openTreasureButton != null)
            {
                openTreasureButton.gameObject.SetActive(false);
            }

            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
            }
        }

        private void OnCloseClicked()
        {
            // Update currency balances
            RunSessionManager.GrantScraps(_rolledScraps);
            RunSessionManager.GrantParts(_rolledParts);

            // Save state explicitly
            SaveManager.SaveRun();

            // Room completion flow
            gameObject.SetActive(false);

            var combatUI = UnityEngine.Object.FindFirstObjectByType<Nevergreen.Prototype.CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                if (!RunSessionManager.RoomCompleted)
                {
                    RunSessionManager.CompleteRoom(new System.Collections.Generic.List<RoomData>());
                }
            }
        }
    }
}
