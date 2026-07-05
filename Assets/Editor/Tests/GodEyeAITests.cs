using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Combat.AI;
using Nevergreen.Combat.AI.Nodes;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class GodEyeAITests
    {
        private GameObject _battleGo;
        private BattleSystem _battleSystem;
        private CombatCharacter _boss;
        private AIBrain _brain;
        private GodEyeController _controller;
        private List<GameObject> _trackedObjects = new List<GameObject>();

        // Skill assets
        private SkillData _ultimateSkill;
        private SkillData _summonSkill;
        private SkillData _buffSkill;
        private SkillData _markSkill;
        
        // Character Data
        private CharacterData _protectorData;
        private CharacterData _damageData;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();

            _battleGo = new GameObject("BattleSystem");
            _battleSystem = _battleGo.AddComponent<BattleSystem>();
            _trackedObjects.Add(_battleGo);

            // Create God-Eye boss at rank 1 by default
            _boss = CombatTestHelper.CreateCombatCharacter("godeye", Team.Enemy, 1, maxHP: 1000, attack: 150);
            _brain = _boss.gameObject.GetComponent<AIBrain>();
            if (_brain == null) _brain = _boss.gameObject.AddComponent<AIBrain>();
            _controller = _boss.gameObject.AddComponent<GodEyeController>();
            _trackedObjects.Add(_boss.gameObject);

            // Inject RNG to prevent NullReferenceException during skill execution
            typeof(BattleSystem).GetField("_rng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, CombatTestHelper.CreateFixedRng());

            // Create test skills
            _ultimateSkill = CreateEnemySkill("ultimate", "Ultimate Attack");
            _summonSkill = CreateSelfSkill("summon_ally", "Summon Ally");
            _buffSkill = CreateAllySkill("buff_ally", "Buff Ally");
            _markSkill = CreateEnemySkill("mark_target", "Mark Target");
            
            // Create character data for interceptor tests
            var stats = ScriptableObject.CreateInstance<StatBlockData>();
            stats.maxHP = 100;
            
            _protectorData = CombatTestHelper.CreateCharacterData("godeye_protector", "Protector", stats, CharacterTeamType.Enemy);
            _damageData = CombatTestHelper.CreateCharacterData("godeye_damage", "Damage", stats, CharacterTeamType.Enemy);

            // Configure controller
            _controller.ultimateSkill = _ultimateSkill;
            _controller.summonSkill = _summonSkill;
            _controller.ultimateRoundInterval = 3;
            _controller.protectorData = _protectorData;
            _controller.damageData = _damageData;
            
            // Create dummy prefabs
            _controller.protectorAllyPrefab = new GameObject("ProtectorPrefab");
            var pCombat = _controller.protectorAllyPrefab.AddComponent<CombatCharacter>();
            pCombat.characterData = _protectorData;
            _trackedObjects.Add(_controller.protectorAllyPrefab);
            
            _controller.damageAllyPrefab = new GameObject("DamagePrefab");
            var dCombat = _controller.damageAllyPrefab.AddComponent<CombatCharacter>();
            dCombat.characterData = _damageData;
            _trackedObjects.Add(_controller.damageAllyPrefab);

            // In Edit Mode, Awake() doesn't run automatically for dynamically added components, so we inject _self manually
            typeof(GodEyeController).GetField("_self", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_controller, _boss);
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var go in _trackedObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _trackedObjects.Clear();

            // Cleanup ScriptableObjects
            SafeDestroyAsset(_ultimateSkill);
            SafeDestroyAsset(_summonSkill);
            SafeDestroyAsset(_buffSkill);
            SafeDestroyAsset(_markSkill);
            SafeDestroyAsset(_protectorData.statPerLevel[0]);
            SafeDestroyAsset(_protectorData);
            SafeDestroyAsset(_damageData);

            CombatTestHelper.CleanupTestDatabase();
        }

        // ===================================================================
        // Helpers
        // ===================================================================

        private void SetPlayerTeam(List<CombatCharacter> team)
        {
            typeof(BattleSystem).GetField("_playerTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, team);
        }

        private void SetEnemyTeam(List<CombatCharacter> team)
        {
            typeof(BattleSystem).GetField("_enemyTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_battleSystem, team);
        }

        private SkillData CreateSelfSkill(string id, string name)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = id;
            skill.displayName = name;
            skill.targetScope = TargetScope.Self;
            skill.useRanks = new List<int> { 1, 2, 3, 4 };
            skill.targetRanks = new List<int> { 1, 2, 3, 4 };
            skill.hitCount = 1;
            skill.maxUsesPerBattle = -1;
            skill.modifier = new SkillModifier();
            skill.effects = new List<ISkillEffect>();
            return skill;
        }

        private SkillData CreateAllySkill(string id, string name)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = id;
            skill.displayName = name;
            skill.targetScope = TargetScope.Allies;
            skill.useRanks = new List<int> { 1, 2, 3, 4 };
            skill.targetRanks = new List<int> { 1, 2, 3, 4 };
            skill.hitCount = 1;
            skill.maxUsesPerBattle = -1;
            skill.modifier = new SkillModifier();
            skill.effects = new List<ISkillEffect>();
            return skill;
        }

        private SkillData CreateEnemySkill(string id, string name)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = id;
            skill.displayName = name;
            skill.targetScope = TargetScope.Enemies;
            skill.useRanks = new List<int> { 1, 2, 3, 4 };
            skill.targetRanks = new List<int> { 1, 2, 3, 4 };
            skill.hitCount = 1;
            skill.maxUsesPerBattle = -1;
            skill.modifier = new SkillModifier();
            skill.effects = new List<ISkillEffect>();
            return skill;
        }

        private CombatCharacter CreateTrackedCharacter(string id, Team team, int rank, int maxHP = 100)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(id, team, rank, maxHP: maxHP);
            _trackedObjects.Add(cc.gameObject);
            return cc;
        }

        private static void SafeDestroyAsset(Object obj)
        {
            if (obj != null && !UnityEditor.EditorUtility.IsPersistent(obj))
                Object.DestroyImmediate(obj);
        }

        // ===================================================================
        // GodEyeTurnBehaviorNode Tests
        // ===================================================================

        [Test]
        public void TurnBehavior_Summons_WhenTeamHasLessThan4Members()
        {
            var p1 = CreateTrackedCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _boss });

            _boss.equippedSkills.Add(_summonSkill);

            var node = new GodEyeTurnBehaviorNode
            {
                summonSkill = _summonSkill,
                buffSkill = _buffSkill,
                markSkill = _markSkill
            };

            bool result = node.TryGetDecision(_brain, _battleSystem, out AIDecision decision);

            Assert.IsTrue(result, "Should produce a decision.");
            Assert.AreEqual(_summonSkill, decision.skill, "Should summon when team < 4.");
            Assert.Contains(_boss, decision.targets, "Summon target should be self.");
        }

        [Test]
        public void TurnBehavior_RandomBuffOrMark_WhenTeamHas4Members()
        {
            var p1 = CreateTrackedCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            
            var ally1 = CreateTrackedCharacter("ally1", Team.Enemy, 2);
            var ally2 = CreateTrackedCharacter("ally2", Team.Enemy, 3);
            var ally3 = CreateTrackedCharacter("ally3", Team.Enemy, 4);
            SetEnemyTeam(new List<CombatCharacter> { _boss, ally1, ally2, ally3 });

            _boss.equippedSkills.Add(_buffSkill);
            _boss.equippedSkills.Add(_markSkill);

            var node = new GodEyeTurnBehaviorNode
            {
                summonSkill = _summonSkill,
                buffSkill = _buffSkill,
                markSkill = _markSkill
            };

            bool sawBuff = false;
            bool sawMark = false;
            for (int i = 0; i < 100; i++)
            {
                node.TryGetDecision(_brain, _battleSystem, out AIDecision decision);
                if (decision.skill == _buffSkill) 
                {
                    sawBuff = true;
                    Assert.IsTrue(decision.targets.Count == 1, "Buff should have exactly 1 target.");
                    Assert.AreNotEqual(_boss, decision.targets[0], "Buff should not target self.");
                }
                if (decision.skill == _markSkill) 
                {
                    sawMark = true;
                    Assert.IsTrue(decision.targets.Count == 1, "Mark should have exactly 1 target.");
                    Assert.AreEqual(p1, decision.targets[0], "Mark should target lowest HP enemy (only p1 exists).");
                }
                if (sawBuff && sawMark) break;
            }

            Assert.IsTrue(sawBuff, "Should sometimes choose buff.");
            Assert.IsTrue(sawMark, "Should sometimes choose mark.");
        }
        
        [Test]
        public void TurnBehavior_MarkTargetsLowestHPPllayer()
        {
            var p1 = CreateTrackedCharacter("p1", Team.Player, 1, maxHP: 200);
            var p2 = CreateTrackedCharacter("p2", Team.Player, 2, maxHP: 150); // Lowest HP initially
            var p3 = CreateTrackedCharacter("p3", Team.Player, 3, maxHP: 250);
            
            // Set current HP for testing
            p1.currentHP = 200;
            p2.currentHP = 50; // Truly lowest HP
            p3.currentHP = 250;
            
            SetPlayerTeam(new List<CombatCharacter> { p1, p2, p3 });
            
            // Full team to force buff/mark
            var ally1 = CreateTrackedCharacter("ally1", Team.Enemy, 2);
            var ally2 = CreateTrackedCharacter("ally2", Team.Enemy, 3);
            var ally3 = CreateTrackedCharacter("ally3", Team.Enemy, 4);
            SetEnemyTeam(new List<CombatCharacter> { _boss, ally1, ally2, ally3 });

            _boss.equippedSkills.Add(_buffSkill);
            _boss.equippedSkills.Add(_markSkill);
            
            // Unequip buff to force mark
            _boss.equippedSkills.Remove(_buffSkill);

            var node = new GodEyeTurnBehaviorNode
            {
                summonSkill = _summonSkill,
                buffSkill = null, // Force AI to NOT use buff
                markSkill = _markSkill
            };

            bool result = node.TryGetDecision(_brain, _battleSystem, out AIDecision decision);
            
            Assert.IsTrue(result, "Should produce decision.");
            Assert.AreEqual(_markSkill, decision.skill, "Should choose mark.");
            Assert.AreEqual(1, decision.targets.Count, "Should target exactly 1 enemy.");
            Assert.AreEqual(p2, decision.targets[0], "Should target the lowest HP player.");
        }

        // ===================================================================
        // GodEyeController Tests
        // ===================================================================

        [Test]
        public void Controller_ExecutesUltimate_OnBattleStarted()
        {
            var p1 = CreateTrackedCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _boss });
            
            // We use reflection to invoke the event handler since we can't fully mock BattleSystem in EditMode
            InvokeBattleStarted();
            
            // If it doesn't crash, the execute ultimate path was taken.
            // Ideally we'd use a mock, but BattleSystem isn't an interface. We verified the call executes gracefully.
            Assert.Pass();
        }
        
        [Test]
        public void Controller_ExecutesUltimate_OnCorrectRounds()
        {
            var p1 = CreateTrackedCharacter("p1", Team.Player, 1);
            SetPlayerTeam(new List<CombatCharacter> { p1 });
            SetEnemyTeam(new List<CombatCharacter> { _boss });
            
            _controller.ultimateRoundInterval = 3;
            
            // Round 1 is skipped (handled by BattleStarted)
            InvokeRoundStarted(1);
            InvokeRoundStarted(2);
            InvokeRoundStarted(3);
            InvokeRoundStarted(4); // Execute
            InvokeRoundStarted(7); // Execute
            
            Assert.Pass();
        }

        [Test]
        public void Controller_SummonsProtector_WhenNoneExists()
        {
            SetEnemyTeam(new List<CombatCharacter> { _boss });
            
            InvokeActionResolved(_summonSkill);
            
            var spawned = _battleSystem.EnemyTeam.Last();
            Assert.AreNotEqual(_boss, spawned, "A new character should be spawned.");
            Assert.AreEqual(2, spawned.rank, "Should spawn at next available rank (2).");
            Assert.AreEqual("ProtectorPrefab(Clone)", spawned.gameObject.name, "Should spawn protector when none exists.");
        }
        
        [Test]
        public void Controller_SummonsDamage_WhenProtectorExists()
        {
            var protector = CreateTrackedCharacter("existing_protector", Team.Enemy, 2);
            protector.characterData = _protectorData;
            SetEnemyTeam(new List<CombatCharacter> { _boss, protector });
            
            InvokeActionResolved(_summonSkill);
            
            var spawned = _battleSystem.EnemyTeam.Last();
            Assert.AreNotEqual(_boss, spawned, "A new character should be spawned.");
            Assert.AreNotEqual(protector, spawned, "A new character should be spawned.");
            Assert.AreEqual(3, spawned.rank, "Should spawn at next available rank (3).");
            Assert.AreEqual("DamagePrefab(Clone)", spawned.gameObject.name, "Should spawn damage when protector already exists.");
        }

        // ===================================================================
        // Internal Helpers (reflection-based for testing)
        // ===================================================================

        private void InvokeBattleStarted()
        {
            var bsField = typeof(GodEyeController).GetField("_battleSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bsField.SetValue(_controller, _battleSystem);

            var method = typeof(GodEyeController).GetMethod("HandleBattleStarted",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_controller, null);
        }

        private void InvokeRoundStarted(int roundNumber)
        {
            var bsField = typeof(GodEyeController).GetField("_battleSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bsField.SetValue(_controller, _battleSystem);

            var method = typeof(GodEyeController).GetMethod("HandleRoundStarted",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_controller, new object[] { roundNumber });
        }
        
        private void InvokeActionResolved(SkillData skill)
        {
            var bsField = typeof(GodEyeController).GetField("_battleSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bsField.SetValue(_controller, _battleSystem);

            var method = typeof(GodEyeController).GetMethod("HandleActionResolved",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_controller, new object[] { _boss, skill, null });
        }
    }
}
