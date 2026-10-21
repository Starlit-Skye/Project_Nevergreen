using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;
using Nevergreen.UI;
using TMPro;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class ExhaustionTests
    {
        private List<GameObject> _cleanupGos;
        private List<ScriptableObject> _cleanupSos;
        private string _testRunSavePath;
        private string _testProfileSavePath;

        [SetUp]
        public void SetUp()
        {
            _cleanupGos = new List<GameObject>();
            _cleanupSos = new List<ScriptableObject>();

            CombatTestHelper.InitializeTestDatabase();
            RunSessionManager.ClearAll();

            _testRunSavePath = Path.Combine(Application.temporaryCachePath, "exhaustion_test_run.dat");
            _testProfileSavePath = Path.Combine(Application.temporaryCachePath, "exhaustion_test_profile.dat");
            SaveManager.SetSavePathsForTesting(_testRunSavePath, _testProfileSavePath);

            if (File.Exists(_testRunSavePath)) File.Delete(_testRunSavePath);
            if (File.Exists(_testProfileSavePath)) File.Delete(_testProfileSavePath);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanupGos)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            foreach (var so in _cleanupSos)
            {
                if (so != null && !UnityEditor.EditorUtility.IsPersistent(so))
                {
                    ScriptableObject.DestroyImmediate(so, true);
                }
            }

            if (!string.IsNullOrEmpty(_testRunSavePath) && File.Exists(_testRunSavePath))
            {
                File.Delete(_testRunSavePath);
            }

            if (!string.IsNullOrEmpty(_testProfileSavePath) && File.Exists(_testProfileSavePath))
            {
                File.Delete(_testProfileSavePath);
            }

            SaveManager.SetSavePathsForTesting(null, null);
            RunSessionManager.ClearAll();
            CombatTestHelper.CleanupTestDatabase();
        }

        private T TrackSo<T>(T so) where T : ScriptableObject
        {
            if (so != null) _cleanupSos.Add(so);
            return so;
        }

        private GameObject TrackGo(GameObject go)
        {
            if (go != null) _cleanupGos.Add(go);
            return go;
        }

        // ============================================================
        // 1. Counter & Limit Math
        // ============================================================

        [Test]
        public void AdvanceExhaustion_IncrementsAndClampsAtLimit()
        {
            RunSessionManager.ExhaustionLimit = 3;
            RunSessionManager.ExhaustionRound = 0;

            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual(1, RunSessionManager.ExhaustionRound);
            Assert.IsFalse(RunSessionManager.IsExhausted);

            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual(2, RunSessionManager.ExhaustionRound);
            Assert.IsFalse(RunSessionManager.IsExhausted);

            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual(3, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(RunSessionManager.IsExhausted);

            // Once capped, it does not exceed the limit
            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual(3, RunSessionManager.ExhaustionRound);
            Assert.IsTrue(RunSessionManager.IsExhausted);
        }

        [Test]
        public void AdvanceExhaustion_NoOp_WhenLimitZero()
        {
            RunSessionManager.ExhaustionLimit = 0;
            RunSessionManager.ExhaustionRound = 0;

            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual(0, RunSessionManager.ExhaustionRound);
            Assert.IsFalse(RunSessionManager.IsExhausted);
        }

        [Test]
        public void IsExhausted_False_WhenLimitZero()
        {
            RunSessionManager.ExhaustionLimit = 0;
            RunSessionManager.ExhaustionRound = 5;

            Assert.IsFalse(RunSessionManager.IsExhausted, "When limit is <= 0, exhaustion mechanic is disabled.");
        }

        [Test]
        public void BattleSystem_OnRoundEnded_AdvancesExhaustion()
        {
            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 1;

            var bsGo = TrackGo(new GameObject("BattleSystem"));
            var battleSystem = bsGo.AddComponent<BattleSystem>();

            RunSessionManager.SubscribeToBattle(battleSystem);

            var roundEndedField = typeof(BattleSystem).GetField("OnRoundEnded",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = roundEndedField?.GetValue(battleSystem) as System.Action<int>;
            handler?.Invoke(1);

            Assert.AreEqual(2, RunSessionManager.ExhaustionRound, "Exhaustion round should advance when a round ends.");
        }

        // ============================================================
        // 2. CombatConfig Tier Mapping
        // ============================================================

        [Test]
        public void GetExhaustionLimit_TrivialAndEarly_ShareLimit()
        {
            var config = TrackSo(ScriptableObject.CreateInstance<CombatConfig>());
            config.earlyExhaustionLimit = 8;
            config.midExhaustionLimit = 8;
            config.lateExhaustionLimit = 10;
            config.bossExhaustionLimit = 12;

            Assert.AreEqual(8, config.GetExhaustionLimit(EnemyEncounterTier.Trivial));
            Assert.AreEqual(8, config.GetExhaustionLimit(EnemyEncounterTier.EarlyGame));
        }

        [Test]
        public void GetExhaustionLimit_MapsMidLateBoss()
        {
            var config = TrackSo(ScriptableObject.CreateInstance<CombatConfig>());
            config.earlyExhaustionLimit = 8;
            config.midExhaustionLimit = 8;
            config.lateExhaustionLimit = 10;
            config.bossExhaustionLimit = 12;

            Assert.AreEqual(8, config.GetExhaustionLimit(EnemyEncounterTier.MidGame));
            Assert.AreEqual(10, config.GetExhaustionLimit(EnemyEncounterTier.LateGame));
            Assert.AreEqual(12, config.GetExhaustionLimit(EnemyEncounterTier.Boss));
            Assert.AreEqual(0, config.GetExhaustionLimit(EnemyEncounterTier.Elite), "Elite tier returns 0 (callers use progression tier).");
        }

        // ============================================================
        // 3. Room & Progression Lifecycle
        // ============================================================

        [Test]
        public void OnSceneLoaded_HealRoom_ResetsExhaustion()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.ExhaustionRound = 6;
            RunSessionManager.ExhaustionLimit = 8;

            var healRoom = TrackSo(ScriptableObject.CreateInstance<RoomData>());
            healRoom.roomId = "RD_HealRoom";
            RunSessionManager.NextRoomData = healRoom;

            RunSessionManager.OnSceneLoaded("CombatPrototype");

            Assert.AreEqual(0, RunSessionManager.ExhaustionRound, "Entering HealRoom should reset ExhaustionRound to 0.");
            Assert.AreEqual(0, RunSessionManager.PreCombatExhaustionRound);
        }

        [Test]
        public void OnSceneLoaded_CombatRoom_PersistsExhaustionAcrossBattles()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.ExhaustionRound = 4;

            var combatRoom = TrackSo(ScriptableObject.CreateInstance<RoomData>());
            combatRoom.roomId = "RD_CombatRoom";
            RunSessionManager.NextRoomData = combatRoom;

            RunSessionManager.OnSceneLoaded("CombatPrototype");

            Assert.AreEqual(4, RunSessionManager.ExhaustionRound, "Combat room should retain ExhaustionRound across battles.");
            Assert.AreEqual(4, RunSessionManager.PreCombatExhaustionRound);
        }

        [Test]
        public void OnSceneLoaded_TrivialToEarly_DoesNotReset()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.RoomProgression = 1; // Room 1 -> Trivial
            RunSessionManager.ExhaustionRound = 3;

            var room2 = TrackSo(ScriptableObject.CreateInstance<RoomData>());
            room2.roomId = "RD_CombatRoom_2";
            RunSessionManager.NextRoomData = room2;

            RunSessionManager.OnSceneLoaded("CombatPrototype");

            // Progressed to Room 2 (EarlyGame tier)
            Assert.AreEqual(2, RunSessionManager.RoomProgression);
            Assert.AreEqual(3, RunSessionManager.ExhaustionRound, "Exhaustion does not reset when transitioning between Trivial and EarlyGame.");
        }

        [Test]
        public void OnSceneLoaded_EliteRoom_UsesProgressionTierLimit()
        {
            var config = GameDatabase.Instance.CombatConfig;
            config.earlyExhaustionLimit = 8;
            config.midExhaustionLimit = 8;

            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.RoomProgression = 2; // Early game progression

            var eliteRoom = TrackSo(ScriptableObject.CreateInstance<RoomData>());
            eliteRoom.roomId = "RD_EliteRoom";
            eliteRoom.strategy = new EliteEncounterRoomEffectStrategy();
            RunSessionManager.NextRoomData = eliteRoom;

            RunSessionManager.OnSceneLoaded("CombatPrototype");

            Assert.AreEqual(8, RunSessionManager.ExhaustionLimit, "Elite room must resolve limit via progression tier (Early = 8), not Elite tier.");
        }

        // ============================================================
        // 4. Healing Gate & Mechanics
        // ============================================================

        [Test]
        public void Heal_PlayerExhausted_HealsZero()
        {
            var player = TrackGo(new GameObject("Player")).AddComponent<CombatCharacter>();
            var stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var charData = CombatTestHelper.CreateCharacterData("p1", "Player1", stats, CharacterTeamType.Player);
            TrackSo(stats);
            TrackSo(charData);
            player.characterData = charData;
            player.InitializeForCombat(Team.Player, 1);
            player.TakeDamage(50);
            Assert.AreEqual(50, player.currentHP);

            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 5;
            Assert.IsTrue(RunSessionManager.IsExhausted);

            int healed = player.Heal(30);

            Assert.AreEqual(0, healed, "Exhausted player should receive 0 healing.");
            Assert.AreEqual(50, player.currentHP, "Exhausted player HP should not change.");
        }

        [Test]
        public void Heal_EnemyExhausted_StillHeals()
        {
            var enemy = TrackGo(new GameObject("Enemy")).AddComponent<CombatCharacter>();
            var stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var charData = CombatTestHelper.CreateCharacterData("e1", "Enemy1", stats, CharacterTeamType.Enemy);
            TrackSo(stats);
            TrackSo(charData);
            enemy.characterData = charData;
            enemy.InitializeForCombat(Team.Enemy, 1);
            enemy.TakeDamage(50);
            Assert.AreEqual(50, enemy.currentHP);

            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 5;
            Assert.IsTrue(RunSessionManager.IsExhausted);

            int healed = enemy.Heal(30);

            Assert.AreEqual(30, healed, "Enemy is not affected by run exhaustion.");
            Assert.AreEqual(80, enemy.currentHP);
        }

        [Test]
        public void RestoreHOT_PlayerExhausted_NoHeal()
        {
            var player = TrackGo(new GameObject("Player")).AddComponent<CombatCharacter>();
            var stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var charData = CombatTestHelper.CreateCharacterData("p1", "Player1", stats, CharacterTeamType.Player);
            TrackSo(stats);
            TrackSo(charData);
            player.characterData = charData;
            player.InitializeForCombat(Team.Player, 1);
            player.TakeDamage(50);

            player.AddStatus(new StatusEffectInstance(StatusType.Restore, StatTarget.MaxHP, 20, 2));

            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 5;

            StatusProcessor.ProcessPeriodicEffects(player);

            Assert.AreEqual(50, player.currentHP, "Periodic Restore should not heal exhausted player.");
        }

        [Test]
        public void HealGuardianEffect_PlayerExhausted_NoHeal()
        {
            var guardian = TrackGo(new GameObject("Guardian")).AddComponent<CombatCharacter>();
            var gStats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var gData = CombatTestHelper.CreateCharacterData("g1", "Guardian1", gStats, CharacterTeamType.Player);
            TrackSo(gStats);
            TrackSo(gData);
            guardian.characterData = gData;
            guardian.InitializeForCombat(Team.Player, 1);
            guardian.TakeDamage(50);

            var guarded = TrackGo(new GameObject("Guarded")).AddComponent<CombatCharacter>();
            var tStats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var tData = CombatTestHelper.CreateCharacterData("t1", "Guarded1", tStats, CharacterTeamType.Player);
            TrackSo(tStats);
            TrackSo(tData);
            guarded.characterData = tData;
            guarded.InitializeForCombat(Team.Player, 2);

            guarded.AddStatus(new GuardStatusInstance(guardian, 3));

            var effect = new HealGuardianEffect { ignoreMiss = true };
            var skill = TrackSo(ScriptableObject.CreateInstance<SkillData>());
            skill.skillId = "test_hg";
            skill.modifier = new SkillModifier { healPercent = 1.0f };
            skill.targetScope = TargetScope.Allies;
            skill.effects.Add(effect);

            var bsGo = TrackGo(new GameObject("BattleSystem"));
            var bs = bsGo.AddComponent<BattleSystem>();

            var ctx = new SkillContext(guarded, skill, new List<CombatCharacter> { guarded }, bs, CombatTestHelper.CreateFixedRng());

            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 5;

            effect.Execute(ctx, guarded);

            Assert.AreEqual(50, guardian.currentHP, "Guardian should not heal while player team is exhausted.");
            Assert.AreEqual(0, ctx.calculatedValue);
        }

        [Test]
        public void ReviveFromDowned_PlayerExhausted_StillRestoresHP()
        {
            var player = TrackGo(new GameObject("Player")).AddComponent<CombatCharacter>();
            var stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var charData = CombatTestHelper.CreateCharacterData("p1", "Player1", stats, CharacterTeamType.Player);
            TrackSo(stats);
            TrackSo(charData);
            player.characterData = charData;
            player.InitializeForCombat(Team.Player, 1);
            player.currentHP = 0;
            player.EnterDownedState();
            Assert.AreEqual(LifeState.Downed, player.state);

            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 5;
            Assert.IsTrue(RunSessionManager.IsExhausted);

            player.ReviveFromDowned(25);

            Assert.AreEqual(LifeState.Alive, player.state);
            Assert.AreEqual(25, player.currentHP, "Revive is not affected by exhaustion and successfully restores HP.");
        }

        // ============================================================
        // 5. Safety: Never Damage on Heal
        // ============================================================

        [Test]
        public void Heal_NegativeAmount_NeverReducesHP()
        {
            var character = TrackGo(new GameObject("Character")).AddComponent<CombatCharacter>();
            var stats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var charData = CombatTestHelper.CreateCharacterData("c1", "Char1", stats, CharacterTeamType.Player);
            TrackSo(stats);
            TrackSo(charData);
            character.characterData = charData;
            character.InitializeForCombat(Team.Player, 1);
            character.TakeDamage(30);
            Assert.AreEqual(70, character.currentHP);

            int healed = character.Heal(-25);

            Assert.AreEqual(0, healed, "Negative heal amount must return 0.");
            Assert.AreEqual(70, character.currentHP, "Negative heal amount must never reduce HP.");
        }

        [Test]
        public void HealEffect_HealReductionOver100_NeverDamages()
        {
            var bsGo = TrackGo(new GameObject("BattleSystem"));
            var battleSystem = bsGo.AddComponent<BattleSystem>();

            var target = TrackGo(new GameObject("Target")).AddComponent<CombatCharacter>();
            var tStats = CombatTestHelper.CreateStatBlock(maxHP: 100);
            var tData = CombatTestHelper.CreateCharacterData("t1", "Target1", tStats, CharacterTeamType.Player);
            TrackSo(tStats);
            TrackSo(tData);
            target.characterData = tData;
            target.InitializeForCombat(Team.Player, 1);
            target.TakeDamage(50);
            Assert.AreEqual(50, target.currentHP);

            // Debuff of 150% heal reduction
            var debuff = new HealReceivedDebuffStatusInstance(battleSystem, 150, 3);
            target.AddStatus(debuff);

            var healer = TrackGo(new GameObject("Healer")).AddComponent<CombatCharacter>();
            var hStats = CombatTestHelper.CreateStatBlock(attack: 50, maxHP: 100);
            var hData = CombatTestHelper.CreateCharacterData("h1", "Healer1", hStats, CharacterTeamType.Player);
            TrackSo(hStats);
            TrackSo(hData);
            healer.characterData = hData;
            healer.InitializeForCombat(Team.Player, 2);

            var healSkill = TrackSo(ScriptableObject.CreateInstance<SkillData>());
            healSkill.skillId = "heal_over_reduction";
            healSkill.modifier = new SkillModifier { healPercent = 1.0f };
            var healEffect = new HealEffect();
            healSkill.effects.Add(healEffect);

            var ctx = new SkillContext(healer, healSkill, new List<CombatCharacter> { target }, battleSystem, CombatTestHelper.CreateFixedRng());

            var eventField = typeof(BattleSystem).GetField("OnBeforeDamageCalculation",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handler = eventField.GetValue(battleSystem) as System.Action<SkillContext>;
            handler?.Invoke(ctx);

            healEffect.Execute(ctx, target);

            Assert.AreEqual(50, target.currentHP, "Over 100% heal reduction must never damage the target.");
            Assert.AreEqual(0, ctx.calculatedValue);
        }

        // ============================================================
        // 6. Save Persistence
        // ============================================================

        [Test]
        public void SaveRun_MidBattle_SavesPreCombatExhaustionRound()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.PreCombatExhaustionRound = 2;
            RunSessionManager.ExhaustionRound = 5; // Advanced during combat

            SaveManager.SaveRun();

            RunSessionManager.Clear();
            SaveManager.LoadRun();

            Assert.AreEqual(2, RunSessionManager.ExhaustionRound, "Mid-battle save should save PreCombatExhaustionRound so resuming restores pre-combat state.");
            Assert.AreEqual(2, RunSessionManager.PreCombatExhaustionRound);
        }

        [Test]
        public void LoadRun_RestoresExhaustionRound()
        {
            RunSessionManager.CurrentParty.Add(new PartyMemberInfo());
            RunSessionManager.PreCombatExhaustionRound = 4;
            RunSessionManager.ExhaustionRound = 4;

            SaveManager.SaveRun();

            RunSessionManager.Clear();
            Assert.AreEqual(0, RunSessionManager.ExhaustionRound);

            bool loaded = SaveManager.LoadRun();
            Assert.IsTrue(loaded);
            Assert.AreEqual(4, RunSessionManager.ExhaustionRound);
            Assert.AreEqual(4, RunSessionManager.PreCombatExhaustionRound);
        }

        // ============================================================
        // 7. Exhaustion Event & UI Display
        // ============================================================

        [Test]
        public void OnExhaustionChanged_FiresOnAdvance()
        {
            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 0;

            int changeCount = 0;
            RunSessionManager.OnExhaustionChanged += () => changeCount++;

            RunSessionManager.AdvanceExhaustion();

            Assert.AreEqual(1, changeCount, "OnExhaustionChanged should fire when AdvanceExhaustion changes round.");
            Assert.AreEqual(1, RunSessionManager.ExhaustionRound);
        }

        [Test]
        public void OnExhaustionChanged_FiresOnLimitChange()
        {
            int changeCount = 0;
            RunSessionManager.OnExhaustionChanged += () => changeCount++;

            RunSessionManager.ExhaustionLimit = 10;

            Assert.AreEqual(1, changeCount, "OnExhaustionChanged should fire when ExhaustionLimit changes.");
        }

        [Test]
        public void OnExhaustionChanged_DoesNotFire_WhenValueUnchanged()
        {
            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 2;

            int changeCount = 0;
            RunSessionManager.OnExhaustionChanged += () => changeCount++;

            // Set identical values
            RunSessionManager.ExhaustionLimit = 5;
            RunSessionManager.ExhaustionRound = 2;

            Assert.AreEqual(0, changeCount, "OnExhaustionChanged should not fire if the value did not change.");
        }

        [Test]
        public void ExhaustionDisplayUI_ShowsFormattedText()
        {
            var go = TrackGo(new GameObject("ExhaustionUI"));
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var displayUI = go.AddComponent<ExhaustionDisplayUI>();
            displayUI.textMesh = tmp;
            InvokeLifecycle(displayUI, "OnEnable");

            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 3;

            Assert.AreEqual("Exhaustion: 3/8", tmp.text, "Display should update to 'Exhaustion: 3/8'.");

            RunSessionManager.AdvanceExhaustion();
            Assert.AreEqual("Exhaustion: 4/8", tmp.text, "Display should update to 'Exhaustion: 4/8' after advance.");

            InvokeLifecycle(displayUI, "OnDisable");
        }

        [Test]
        public void ExhaustionDisplayUI_UnsubscribesOnDisable()
        {
            var go = TrackGo(new GameObject("ExhaustionUI"));
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var displayUI = go.AddComponent<ExhaustionDisplayUI>();
            displayUI.textMesh = tmp;
            InvokeLifecycle(displayUI, "OnEnable");

            RunSessionManager.ExhaustionLimit = 8;
            RunSessionManager.ExhaustionRound = 2;
            Assert.AreEqual("Exhaustion: 2/8", tmp.text);

            InvokeLifecycle(displayUI, "OnDisable");

            RunSessionManager.ExhaustionRound = 5;
            Assert.AreEqual("Exhaustion: 2/8", tmp.text, "Display should not update while disabled.");

            InvokeLifecycle(displayUI, "OnEnable");
            Assert.AreEqual("Exhaustion: 5/8", tmp.text, "Display should refresh upon being re-enabled.");

            InvokeLifecycle(displayUI, "OnDisable");
        }

        private static void InvokeLifecycle(MonoBehaviour mb, string methodName)
        {
            var method = mb.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(mb, null);
        }
    }
}
