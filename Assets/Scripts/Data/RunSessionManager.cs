using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen
{
    /// <summary>
    /// Static session manager that persists party and encounter configuration across scenes.
    /// Populated by the Main Menu skill selection, consumed by CombatCharacter.InitializeForCombat.
    /// </summary>
    public static class RunSessionManager
    {
        public static event System.Action OnPartsChanged;
        public static event System.Action OnScrapsChanged;

        /// <summary>The active roster for the current run.</summary>
        public static List<PartyMemberInfo> CurrentParty { get; set; } = new List<PartyMemberInfo>();

        /// <summary>The last formation selected, used to prevent consecutive duplicates.</summary>
        public static EnemyFormationData LastSelectedFormation { get; set; }

        /// <summary>The player's current number of Parts in this run.</summary>
        public static int Parts { get; set; }

        /// <summary>The player's current number of Scraps in this run.</summary>
        public static int Scraps { get; set; }

        /// <summary>Grants Parts to the player's run balance.</summary>
        public static void GrantParts(int amount)
        {
            if (amount > 0)
            {
                Parts += amount;
                OnPartsChanged?.Invoke();
            }
        }

        /// <summary>Attempts to spend Parts, returning true if successful.</summary>
        public static bool TrySpendParts(int amount)
        {
            if (amount < 0) return false;
            if (Parts >= amount)
            {
                Parts -= amount;
                OnPartsChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Grants Scraps to the player's run balance.</summary>
        public static void GrantScraps(int amount)
        {
            if (amount > 0)
            {
                Scraps += amount;
                OnScrapsChanged?.Invoke();
            }
        }

        /// <summary>Attempts to spend Scraps, returning true if successful.</summary>
        public static bool TrySpendScraps(int amount)
        {
            if (amount < 0) return false;
            if (Scraps >= amount)
            {
                Scraps -= amount;
                OnScrapsChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>The RoomData selected by the player for the next room. Persists across scene loads.</summary>
        public static RoomData NextRoomData { get; set; }

        /// <summary>The RoomData for the current active room. Tracks the room the player is currently in.</summary>
        public static RoomData CurrentRoomData { get; set; }

        /// <summary>Checks if a given room is considered a Healing Room based on its ID.</summary>
        public static bool IsHealRoom(RoomData room)
        {
            return room != null && room.roomId == "RD_HealRoom";
        }

        /// <summary>Whether the current room has been completed (victory screen active).</summary>
        public static bool RoomCompleted { get; set; }

        /// <summary>The dynamically generated choices for the next room, if any.</summary>
        public static List<RoomData> NextRoomChoices { get; set; } = new List<RoomData>();

        /// <summary>The number of rooms the player has progressed through in the current run.</summary>
        public static int RoomProgression { get; set; }

        /// <summary>
        /// When true, the next combat scene load will skip incrementing RoomProgression.
        /// Set by the Continue button flow to prevent double-counting a restored room.
        /// </summary>
        public static bool IsResumingRun { get; set; }

        /// <summary>
        /// When true, the next combat scene load will use the saved LastSelectedFormation instead of generating a new random one.
        /// </summary>
        public static bool ShouldUseSavedFormation { get; set; }

        /// <summary>
        /// Tracks boss formation selection probabilities keyed by formationId.
        /// This is meta-progression data that persists across runs.
        /// </summary>
        public static Dictionary<string, float> BossFormationChances
        {
            get
            {
                if (!_bossChancesLoaded)
                {
                    _bossChancesLoaded = true;
                    var profile = SaveManager.LoadProfile();
                    if (profile != null && profile.bossChances != null)
                    {
                        _bossFormationChances.Clear();
                        foreach (var bc in profile.bossChances)
                        {
                            if (!string.IsNullOrEmpty(bc.formationId))
                            {
                                _bossFormationChances[bc.formationId] = bc.chance;
                            }
                        }
                    }
                }
                return _bossFormationChances;
            }
        }
        private static bool _bossChancesLoaded = false;
        private static Dictionary<string, float> _bossFormationChances = new Dictionary<string, float>();

        /// <summary>Invoked when the current room is completed.</summary>
        public static event System.Action RoomComplete;

        private static System.Random _rng = new System.Random();
        private static BattleSystem _activeBattleSystem;

        static RunSessionManager()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.Application.quitting += OnApplicationQuit;
        }

        private static void OnApplicationQuit()
        {
            if (CurrentParty != null && CurrentParty.Count > 0)
            {
                SaveManager.SaveRun();
            }
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            OnSceneLoaded(scene.name);
        }

        public static void OnSceneLoaded(string sceneName)
        {
            if (sceneName == "CombatPrototype" && CurrentParty != null && CurrentParty.Count > 0)
            {
                CurrentRoomData = NextRoomData;

                if (IsResumingRun)
                {
                    // Skip increment — progression was already restored from save
                    IsResumingRun = false;
                }
                else
                {
                    if (!IsHealRoom(CurrentRoomData))
                    {
                        RoomProgression++;
                    }
                    RoomCompleted = false;
                    NextRoomChoices.Clear();
                }

                foreach (var member in CurrentParty)
                {
                    if (member != null)
                    {
                        member.preCombatHP = member.currentHP;
                    }
                }

                SaveManager.SaveRun();
            }
        }
        /// <summary>
        /// Initializes session state for a new run.
        /// Called by the Main Menu bootstrapper before loading the combat scene.
        /// </summary>
        public static void Initialize()
        {
            LastSelectedFormation = null;
            Parts = 0;
            Scraps = 0;
            RoomProgression = 0;
            RoomCompleted = false;
            CurrentRoomData = null;
            NextRoomChoices.Clear();

            if (GameDatabase.Instance != null && GameDatabase.Instance.RoomDatabase != null && GameDatabase.Instance.RoomDatabase.availableRooms != null)
            {
                var marionetteRoom = GameDatabase.Instance.RoomDatabase.availableRooms.Find(r => r != null && r.roomId == "RD_MarionetteRoom");
                if (marionetteRoom != null)
                {
                    NextRoomData = marionetteRoom;
                }
            }

            SaveManager.SaveRun();
        }

        /// <summary>
        /// Marks the current room as completed, stores the generated room choices,
        /// synchronizes pre-combat HP to current post-combat HP, and saves the run.
        /// </summary>
        public static void CompleteRoom(List<RoomData> choices)
        {
            NextRoomChoices = new List<RoomData>(choices);
            RoomCompleted = true;
            RoomComplete?.Invoke();

            if (CurrentParty != null)
            {
                foreach (var member in CurrentParty)
                {
                    if (member != null)
                    {
                        member.preCombatHP = member.currentHP;
                    }
                }
            }

            SaveManager.SaveRun();
        }

        /// <summary>
        /// Subscribes to the BattleSystem's OnBattleEnded event to handle victory room effects.
        /// Called by CombatSceneBootstrap.InitializeBattle().
        /// </summary>
        public static void SubscribeToBattle(BattleSystem battleSystem)
        {
            // Unsubscribe from any previous battle system (safety)
            if (_activeBattleSystem != null)
            {
                _activeBattleSystem.OnBattleEnded -= OnBattleEnded;
            }

            _activeBattleSystem = battleSystem;
            _activeBattleSystem.OnBattleEnded += OnBattleEnded;
        }

        private static void OnBattleEnded(BattleOutcome outcome)
        {
            // Unsubscribe immediately to ensure clean lifecycle
            if (_activeBattleSystem != null)
            {
                _activeBattleSystem.OnBattleEnded -= OnBattleEnded;
                _activeBattleSystem = null;
            }

            if (outcome == BattleOutcome.Victory)
            {
                HandleBattleVictory();
                SaveManager.SaveRun();
            }
            else if (outcome == BattleOutcome.Defeat)
            {
                SaveManager.ClearActiveRun();
                Clear();
            }
        }

        /// <summary>
        /// Handles battle victory by activating the current room effect if it is
        /// configured for OnCombatVictory timing.
        /// </summary>
        public static void HandleBattleVictory()
        {
            if (NextRoomData != null && NextRoomData.activationType == RoomActivationType.OnCombatVictory)
            {
                ActivateCurrentRoomEffect();
                NextRoomData = null;
            }
        }

        /// <summary>
        /// Invokes the current room's effect strategy.
        /// </summary>
        public static void ActivateCurrentRoomEffect()
        {
            if (NextRoomData != null)
            {
                NextRoomData.ActivateEffect();
            }
        }

        /// <summary>
        /// Picks a random formation from the centralized GameDatabase, ensuring it is not
        /// the same as the last selected formation (unless only one formation exists).
        /// For the Boss tier with exactly 2 formations, uses weighted probability selection
        /// with a -10%/+10% adjustment per selection.
        /// </summary>
        /// <returns>The selected formation, or null if no database/formations are available.</returns>
        public static EnemyFormationData GetNextRandomFormation(EnemyEncounterTier tier)
        {
            var db = GameDatabase.Instance;
            if (db == null || db.EnemyFormationDatabase == null)
            {
                LastSelectedFormation = null;
                return null;
            }

            var formations = db.EnemyFormationDatabase.GetFormations(tier);
            if (formations == null || formations.Count == 0)
            {
                LastSelectedFormation = null;
                return null;
            }

            // Only one formation available — no choice
            if (formations.Count == 1)
            {
                LastSelectedFormation = formations[0];
                return LastSelectedFormation;
            }

            // Boss tier with exactly 2 formations: use weighted probability selection
            if (tier == EnemyEncounterTier.Boss && formations.Count == 2)
            {
                return SelectBossFormationWeighted(formations[0], formations[1]);
            }

            // Pick randomly, excluding the last selected
            EnemyFormationData selected;
            int attempts = 0;
            do
            {
                int index = _rng.Next(formations.Count);
                selected = formations[index];
                attempts++;
            }
            while (selected == LastSelectedFormation && attempts < 100);

            LastSelectedFormation = selected;
            return selected;
        }

        /// <summary>
        /// Selects one of two boss formations using weighted probabilities.
        /// Each starts at 50%. When selected, the chosen formation's chance decreases by 10%
        /// and the other's increases by 10%. Probabilities are clamped to [0, 1].
        /// </summary>
        private static EnemyFormationData SelectBossFormationWeighted(EnemyFormationData bossA, EnemyFormationData bossB)
        {
            string idA = bossA.formationId;
            string idB = bossB.formationId;

            // Initialize to 50/50 if not present
            if (!BossFormationChances.ContainsKey(idA))
                BossFormationChances[idA] = 0.5f;
            if (!BossFormationChances.ContainsKey(idB))
                BossFormationChances[idB] = 0.5f;

            float chanceA = BossFormationChances[idA];
            float chanceB = BossFormationChances[idB];
            float total = chanceA + chanceB;

            // Roll weighted random
            float roll = (float)(_rng.NextDouble() * total);
            EnemyFormationData selected;
            EnemyFormationData other;
            string selectedId;
            string otherId;

            if (roll < chanceA)
            {
                selected = bossA;
                other = bossB;
                selectedId = idA;
                otherId = idB;
            }
            else
            {
                selected = bossB;
                other = bossA;
                selectedId = idB;
                otherId = idA;
            }

            // Adjust probabilities: selected -10%, other +10%, clamp [0, 1]
            BossFormationChances[selectedId] = Mathf.Clamp01(BossFormationChances[selectedId] - 0.10f);
            BossFormationChances[otherId] = Mathf.Clamp01(BossFormationChances[otherId] + 0.10f);

            SaveManager.SaveProfile();

            LastSelectedFormation = selected;
            return selected;
        }

        /// <summary>
        /// Clear all party and encounter data (e.g., on returning to main menu).
        /// Note: BossFormationChances is NOT cleared here — it is meta-progression
        /// data that persists across runs.
        /// </summary>
        public static void Clear()
        {
            CurrentParty.Clear();
            LastSelectedFormation = null;
            NextRoomData = null;
            CurrentRoomData = null;
            RoomProgression = 0;
            IsResumingRun = false;
            ShouldUseSavedFormation = false;
            Parts = 0;
            Scraps = 0;
            RoomCompleted = false;
            NextRoomChoices.Clear();

            if (_activeBattleSystem != null)
            {
                _activeBattleSystem.OnBattleEnded -= OnBattleEnded;
                _activeBattleSystem = null;
            }
        }

        /// <summary>
        /// Clears all state including meta-progression data.
        /// Used only in test teardowns.
        /// </summary>
        public static void ClearAll()
        {
            Clear();
            _bossFormationChances.Clear();
            _bossChancesLoaded = false;
            OnPartsChanged = null;
            OnScrapsChanged = null;
        }
    }
}
