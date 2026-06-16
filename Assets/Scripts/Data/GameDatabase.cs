using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Centralized read-only database ScriptableObject that aggregates all game data.
    /// Access via GameDatabase.Instance from any script.
    /// 
    /// At runtime, call GameDatabase.Initialize(instance) from a bootstrap MonoBehaviour.
    /// In the Editor, the Instance property auto-discovers the asset via AssetDatabase.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Nevergreen/Databases/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Sub-Databases")]
        [SerializeField] private EnemyFormationDatabase enemyFormationDatabase;
        [SerializeField] private MarionetteDatabase marionetteDatabase;
        [SerializeField] private TraitDatabase traitDatabase;
        [SerializeField] private RoomDatabase roomDatabase;

        // --- Read-only accessors ---
        public EnemyFormationDatabase EnemyFormationDatabase => enemyFormationDatabase;
        public MarionetteDatabase MarionetteDatabase => marionetteDatabase;
        public TraitDatabase TraitDatabase => traitDatabase;
        public RoomDatabase RoomDatabase => roomDatabase;

        // --- Singleton ---
        private static GameDatabase _instance;
        private static bool _bypassAutoDiscovery = false;

        public static GameDatabase Instance
        {
            get
            {
                #if UNITY_EDITOR
                if (_instance == null && !_bypassAutoDiscovery)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameDatabase");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>(path);
                    }
                }
                #endif
                return _instance;
            }
        }

        /// <summary>
        /// Called by GameInitializer on Awake to register the database instance at runtime.
        /// </summary>
        public static void Initialize(GameDatabase databaseInstance)
        {
            _instance = databaseInstance;
            _bypassAutoDiscovery = false;
        }

        /// <summary>
        /// Allows EditMode tests to inject a mock GameDatabase without relying on
        /// AssetDatabase or a bootstrap scene.
        /// </summary>
        public static void SetInstanceForTesting(GameDatabase testInstance)
        {
            _instance = testInstance;
            _bypassAutoDiscovery = true;
        }

        /// <summary>
        /// Creates a GameDatabase instance populated with the given sub-databases.
        /// For use in EditMode tests only.
        /// </summary>
        public static GameDatabase CreateForTesting(
            EnemyFormationDatabase enemyFormations = null,
            MarionetteDatabase marionettes = null,
            TraitDatabase traits = null,
            RoomDatabase rooms = null)
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            db.enemyFormationDatabase = enemyFormations;
            db.marionetteDatabase = marionettes;
            db.traitDatabase = traits;
            db.roomDatabase = rooms;
            return db;
        }
    }
}
