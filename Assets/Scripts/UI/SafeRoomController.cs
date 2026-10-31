using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    public class SafeRoomController : MonoBehaviour
    {
        [SerializeField] private Button[] marionetteButtons = new Button[4];
        [SerializeField, Range(0f, 1f)] private float healPercent = 0.5f;

        public float HealPercent
        {
            get => healPercent;
            set => healPercent = Mathf.Clamp01(value);
        }

        void Start()
        {
            Initialize(RunSessionManager.CurrentParty);
        }

        public void Initialize(List<PartyMemberInfo> party)
        {
            int buttonCount = marionetteButtons != null ? marionetteButtons.Length : 0;
            for (int i = 0; i < 4; i++)
            {
                if (i >= buttonCount || marionetteButtons[i] == null) continue;

                if (party != null && i < party.Count && party[i] != null && party[i].character != null)
                {
                    marionetteButtons[i].gameObject.SetActive(true);

                    var tmpText = marionetteButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        tmpText.text = party[i].character.displayName;
                    }

                    int captureIndex = i; // capture loop variable
                    marionetteButtons[i].onClick.RemoveAllListeners();
                    marionetteButtons[i].onClick.AddListener(() => OnMarionetteClicked(party[captureIndex]));
                }
                else
                {
                    marionetteButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnMarionetteClicked(PartyMemberInfo member)
        {
            if (member == null || member.character == null) return;

            int maxHP = member.character.GetStatsForLevel(member.currentLevel).maxHP;
            int currentHP = member.currentHP ?? maxHP;

            int healAmount = Mathf.RoundToInt(maxHP * healPercent);
            currentHP = Mathf.Min(maxHP, currentHP + healAmount);

            if (currentHP >= maxHP)
            {
                member.currentHP = null;
            }
            else
            {
                member.currentHP = currentHP;
            }

            CompleteRoom();
        }

        private void CompleteRoom()
        {
            gameObject.SetActive(false);

            CombatUI combatUI = Object.FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                combatUI.ShowRoomSelectionImmediately();
            }
            else
            {
                Debug.LogError("[SafeRoomController] CombatUI not found!");
            }
        }
    }
}
