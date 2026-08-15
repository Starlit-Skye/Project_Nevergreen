using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nevergreen.Data;
using Nevergreen.Prototype;

namespace Nevergreen.UI
{
    public class MarionetteHealChoiceController : MonoBehaviour
    {
        [SerializeField] private Button[] marionetteButtons = new Button[4];
        [SerializeField] private Button healAllButton;

        public void Initialize(List<PartyMemberInfo> party)
        {

            if (healAllButton != null)
            {
                healAllButton.onClick.RemoveAllListeners();
                healAllButton.onClick.AddListener(() => OnHealAllClicked(party));
            }

            for (int i = 0; i < 4; i++)
            {
                if (marionetteButtons[i] == null) continue;

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
                    marionetteButtons[i].onClick.AddListener(() => OnSingleHealClicked(party[captureIndex]));
                }
                else
                {
                    marionetteButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnSingleHealClicked(PartyMemberInfo member)
        {
            if (member == null || member.character == null) return;

            int maxHP = member.character.GetStatsForLevel(member.currentLevel).maxHP;
            int currentHP = member.currentHP ?? maxHP;

            currentHP = Mathf.Min(maxHP, currentHP + 999);
            
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

        private void OnHealAllClicked(List<PartyMemberInfo> party)
        {
            if (party != null)
            {
                foreach (var member in party)
                {
                    if (member == null || member.character == null) continue;

                    int maxHP = member.character.GetStatsForLevel(member.currentLevel).maxHP;
                    int currentHP = member.currentHP ?? maxHP;

                    int healAmount = Mathf.RoundToInt(maxHP * 0.25f);
                    
                    if (member.currentHP != null) // If it's already full (null), we don't heal
                    {
                        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
                        if (currentHP >= maxHP)
                        {
                            member.currentHP = null;
                        }
                        else
                        {
                            member.currentHP = currentHP;
                        }
                    }
                }
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
                Debug.LogError("[MarionetteHealChoiceController] CombatUI not found!");
            }
        }
    }
}
