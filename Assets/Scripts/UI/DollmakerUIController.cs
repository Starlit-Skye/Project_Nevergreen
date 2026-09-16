using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    public class DollmakerUIController : MonoBehaviour
    {
        [Header("Marionette Selection")]
        [Tooltip("The 4 buttons corresponding to the party slots.")]
        [SerializeField] private Button[] marionetteButtons = new Button[4];
        
        [Header("Trait Displays")]
        [Tooltip("Container for perfection prefabs.")]
        [SerializeField] private Transform perfectionsContainer;
        [Tooltip("Container for imperfection prefabs.")]
        [SerializeField] private Transform imperfectionsContainer;
        [Tooltip("Prefab for a Perfection UI item.")]
        [SerializeField] private GameObject perfectionPrefab;
        [Tooltip("Prefab for an Imperfection UI item.")]
        [SerializeField] private GameObject imperfectionPrefab;
        
        [Header("UI Control")]
        [Tooltip("Close button to finish interaction and proceed.")]
        [SerializeField] private Button closeButton;

        private int _partCost;
        private int _perfectionReplacementOptions;
        private List<PartyMemberInfo> _party;
        private int _selectedMemberIndex = -1;

        public void Initialize(int partCost, int perfectionReplacementOptions, List<PartyMemberInfo> party = null)
        {
            _partCost = partCost;
            _perfectionReplacementOptions = perfectionReplacementOptions;
            _party = party ?? RunSessionManager.CurrentParty;

            if (_party == null) _party = new List<PartyMemberInfo>();

            for (int i = 0; i < marionetteButtons.Length; i++)
            {
                if (marionetteButtons[i] == null) continue;

                if (i < _party.Count && _party[i] != null && _party[i].character != null)
                {
                    marionetteButtons[i].interactable = true;
                    
                    var btnText = marionetteButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = _party[i].character.displayName;
                    }

                    int slotIndex = i; // capture loop variable
                    marionetteButtons[i].onClick.RemoveAllListeners();
                    marionetteButtons[i].onClick.AddListener(() => SelectMarionette(slotIndex));
                }
                else
                {
                    marionetteButtons[i].interactable = false;
                    
                    var btnText = marionetteButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = string.Empty;
                    }

                    marionetteButtons[i].onClick.RemoveAllListeners();
                }
            }

            if (_party.Count > 0)
            {
                SelectMarionette(0);
            }
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

        public void SelectMarionette(int index)
        {
            if (_party == null || index < 0 || index >= _party.Count || _party[index] == null) return;

            _selectedMemberIndex = index;
            PartyMemberInfo member = _party[index];

            ClearContainers();

            if (member.perfections != null && perfectionsContainer != null && perfectionPrefab != null)
            {
                foreach (var trait in member.perfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(perfectionPrefab, perfectionsContainer);
                    
                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"- {trait.displayName}";

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    tooltipTrigger.SetTrait(trait);
                }
            }

            if (member.imperfections != null && imperfectionsContainer != null && imperfectionPrefab != null)
            {
                foreach (var trait in member.imperfections)
                {
                    if (trait == null) continue;
                    GameObject item = Instantiate(imperfectionPrefab, imperfectionsContainer);
                    
                    var label = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.text = $"- {trait.displayName}";

                    var tooltipTrigger = item.GetComponent<TraitTooltipTrigger>();
                    if (tooltipTrigger == null) tooltipTrigger = item.AddComponent<TraitTooltipTrigger>();
                    tooltipTrigger.SetTrait(trait);
                }
            }
        }

        private void ClearContainers()
        {
            if (perfectionsContainer != null)
            {
                foreach (Transform child in perfectionsContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            if (imperfectionsContainer != null)
            {
                foreach (Transform child in imperfectionsContainer)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        private void OnCloseClicked()
        {
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
