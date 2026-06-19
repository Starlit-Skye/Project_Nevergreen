using System.Collections.Generic;
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
        /// <summary>The active roster for the current run.</summary>
        public static List<PartyMemberInfo> CurrentParty { get; set; } = new List<PartyMemberInfo>();

        /// <summary>The last formation selected, used to prevent consecutive duplicates.</summary>
        public static EnemyFormationData LastSelectedFormation { get; set; }

        /// <summary>The player's current number of Parts in this run.</summary>
        public static int Parts { get; set; }

        /// <summary>The RoomData selected by the player for the next room. Persists across scene loads.</summary>
        public static RoomData NextRoomData { get; set; }

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
                if (IsResumingRun)
                {
                    // Skip increment — progression was already restored from save
                    IsResumingRun = false;
                }
                else
                {
                    RoomProgression++;
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
            RoomProgression = 0;
            RoomCompleted = false;
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
        /// </summary>
        /// <returns>The selected formation, or null if no database/formations are available.</returns>
        public static EnemyFormationData GetNextRandomFormation(EnemyEncounterTier tier)
        {
            var db = GameDatabase.Instance;
            if (db == null || db.EnemyFormationDatabase == null)
            {
                return null;
            }

            var formations = db.EnemyFormationDatabase.GetFormations(tier);
            if (formations == null || formations.Count == 0)
            {
                return null;
            }

            // Only one formation available — no choice
            if (formations.Count == 1)
            {
                LastSelectedFormation = formations[0];
                return LastSelectedFormation;
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
        /// Clear all party and encounter data (e.g., on returning to main menu).
        /// </summary>
        public static void Clear()
        {
            CurrentParty.Clear();
            LastSelectedFormation = null;
            Parts = 0;
            NextRoomData = null;
            RoomProgression = 0;
            IsResumingRun = false;
            ShouldUseSavedFormation = false;
            RoomCompleted = false;
            NextRoomChoices.Clear();

            if (_activeBattleSystem != null)
            {
                _activeBattleSystem.OnBattleEnded -= OnBattleEnded;
                _activeBattleSystem = null;
            }
        }
    }
}
