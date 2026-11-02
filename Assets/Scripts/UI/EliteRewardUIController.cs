using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class EliteRewardUIController : MonoBehaviour, ITrinketEquipContainerUI
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
        [SerializeField] private Button closeButton;

        [Header("Trinket UI")]
        [SerializeField] private GameObject trinketUIItemPrefab;
        [SerializeField] private Transform trinketDroppedContainer;
        [SerializeField] private List<GameObject> trinketEquipPanels;

        private TrinketData _rolledTrinket;
        private List<PartyMemberInfo> _party;

        public void Initialize(TrinketData rolledTrinket, List<PartyMemberInfo> party = null)
        {
            _rolledTrinket = rolledTrinket;
            _party = party ?? RunSessionManager.CurrentParty;

            if (_rolledTrinket != null && trinketUIItemPrefab != null && trinketDroppedContainer != null)
            {
                // Clear old drops
                foreach (Transform child in trinketDroppedContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }

                var item = Instantiate(trinketUIItemPrefab, trinketDroppedContainer);
                var uiItem = item.GetComponent<TrinketUIItem>();
                if (uiItem != null)
                {
                    uiItem.Initialize(_rolledTrinket);
                }
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
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnCloseClicked()
        {
            SaveManager.SaveRun();

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
