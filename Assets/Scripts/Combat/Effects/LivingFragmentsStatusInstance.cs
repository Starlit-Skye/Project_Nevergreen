using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Runtime instance of Living Fragments status.
    /// Summons the stored prefabs when the host character is destroyed.
    /// </summary>
    [Serializable]
    public class LivingFragmentsStatusInstance : StatusEffectInstance
    {
        private BattleSystem _battleSystem;
        private List<GameObject> _enemyPrefabs;
        private bool _triggered = false;
        private Team _hostTeam;

        public LivingFragmentsStatusInstance(BattleSystem battleSystem, List<GameObject> enemyPrefabs, int duration) 
            : base(StatusType.LivingFragments, StatTarget.Speed, 1, duration) // StatTarget and amplitude are unused but required by base
        {
            _battleSystem = battleSystem;
            _enemyPrefabs = enemyPrefabs ?? new List<GameObject>();
        }

        public override void OnAdded(CombatCharacter host)
        {
            base.OnAdded(host);
            if (host != null)
            {
                _hostTeam = host.team;
                host.OnStateChanged += HandleStateChanged;
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            // Need to use object.ReferenceEquals because Unity's == null is true after destruction
            if (!object.ReferenceEquals(Host, null))
            {
                Host.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(CombatCharacter character, LifeState newState)
        {
            if (newState == LifeState.Destroyed)
            {
                ExecuteSummons();
            }
        }

        private void ExecuteSummons()
        {
            Debug.Log($"[LivingFragments] ExecuteSummons called. triggered={_triggered}, battleSystem={_battleSystem}");
            if (_triggered) return;
            
            if (_battleSystem == null)
            {
                _battleSystem = UnityEngine.Object.FindFirstObjectByType<BattleSystem>();
            }
            if (_battleSystem == null) return;
            
            _triggered = true;

            var team = _hostTeam == Team.Player ? _battleSystem.PlayerTeam : _battleSystem.EnemyTeam;
            Debug.Log($"[LivingFragments] Team identified: {_hostTeam}, current count: {team.Count}");

            int targetSpawnRank = Host != null ? Host.rank : 1;

            foreach (var prefab in _enemyPrefabs)
            {
                if (prefab == null) continue;

                // 1. Capacity check: Max 4 team members
                if (team.Count(c => c.IsAlive) >= 4)
                {
                    Debug.Log($"[LivingFragmentsStatusInstance] Summon skipped: {_hostTeam} team is at max capacity.");
                    break;
                }

                // 2. Calculate Rank & Instantiate (initially spawn at the back)
                int maxOccupied = team.Count > 0 
                    ? team.Max(c => c.OccupiedRanks.Count > 0 ? c.OccupiedRanks.Max() : c.rank) 
                    : 0;
                int spawnRank = maxOccupied + 1;

                float xPos = _battleSystem.GetXPositionForRank(_hostTeam, spawnRank);
                Vector3 spawnPos = new Vector3(xPos, 0f, 0f);
                GameObject allyGO = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);

                // Orient facing direction
                float originalX = Mathf.Abs(allyGO.transform.localScale.x);
                float facingX = (_hostTeam == Team.Player) ? originalX : -originalX;
                allyGO.transform.localScale = new Vector3(facingX, allyGO.transform.localScale.y, allyGO.transform.localScale.z);

                // 3. Initialize CombatCharacter
                CombatCharacter allyCombat = allyGO.GetComponent<CombatCharacter>();
                if (allyCombat == null)
                {
                    Debug.LogError("[LivingFragmentsStatusInstance] Spawned prefab is missing a CombatCharacter component.");
                    if (Application.isPlaying) UnityEngine.Object.Destroy(allyGO);
                    else UnityEngine.Object.DestroyImmediate(allyGO);
                    continue;
                }

                allyCombat.InitializeForCombat(_hostTeam, spawnRank);

                // 4. Register with BattleSystem
                _battleSystem.RegisterSpawnedCharacter(allyCombat);

                // 5. Positioning Shift
                // Move the newly summoned fragment to targetSpawnRank. 
                // Any existing units will cleanly shift backward.
                _battleSystem.ExecuteMoveAndShift(allyCombat, targetSpawnRank);

                Debug.Log($"[LivingFragmentsStatusInstance] Summoned '{allyCombat.DisplayName}' for {_hostTeam} at rank {targetSpawnRank}.");
                
                targetSpawnRank++;
            }
        }
    }
}
