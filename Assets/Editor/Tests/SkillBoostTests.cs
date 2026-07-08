using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Data;
using Nevergreen.Combat;
using Nevergreen.Combat.Effects;

namespace Nevergreen.Tests
{
    public class SkillBoostTests
    {
        private GameObject _battleSystemObj;
        private BattleSystem _battleSystem;
        private CombatConfig _combatConfig;

        [SetUp]
        public void Setup()
        {
            CombatTestHelper.InitializeTestDatabase();
            _battleSystemObj = new GameObject("BattleSystem");
            _battleSystem = _battleSystemObj.AddComponent<BattleSystem>();
            _combatConfig = ScriptableObject.CreateInstance<CombatConfig>();
        }

        [TearDown]
        public void Teardown()
        {
            CombatTestHelper.CleanupTestDatabase();
            if (_battleSystemObj != null) Object.DestroyImmediate(_battleSystemObj);
            if (_combatConfig != null) ScriptableObject.DestroyImmediate(_combatConfig, true);
        }

        private CombatCharacter CreateCharacter(string name, bool isPlayer)
        {
            var go = new GameObject(name);
            var charComp = go.AddComponent<CombatCharacter>();
            charComp.characterData = ScriptableObject.CreateInstance<CharacterData>();
            charComp.characterData.characterId = name.ToLower();
            charComp.team = isPlayer ? Team.Player : Team.Enemy;
            charComp.baseStats = new CombatStats { maxHP = 100, speed = 5 };
            charComp.currentHP = 100;
            charComp.state = LifeState.Alive;
            return charComp;
        }

        [Test]
        public void ApplySkillBoostEffect_AddsStatusInstance()
        {
            var user = CreateCharacter("User", true);
            var target = CreateCharacter("Target", true);
            
            var targetSkill = ScriptableObject.CreateInstance<SkillData>();
            targetSkill.skillId = "ultimate_slash";

            var effect = new ApplySkillBoostEffect
            {
                targetSkill = targetSkill,
                amplitude = 50,
                duration = 3
            };

            var triggerSkill = ScriptableObject.CreateInstance<SkillData>();
            triggerSkill.guaranteedHit = true;

            var ctx = new SkillContext(user, triggerSkill, new List<CombatCharacter> { target }, _battleSystem, new System.Random());
            ctx.currentHitIndex = 0;
            ctx.primaryTarget = target;
            
            // Force hit
            typeof(SkillContext).GetField("didHit").SetValue(ctx, true);

            effect.Execute(ctx, target);

            Assert.AreEqual(1, target.statusEffects.Count, "Buff should be added");
            var buff = target.statusEffects[0] as SkillBoostStatusInstance;
            Assert.IsNotNull(buff, "Should be SkillBoostStatusInstance");
            Assert.AreEqual("ultimate_slash", buff.targetSkillId);
            Assert.AreEqual(50, buff.customAmplitude);
            
            Object.DestroyImmediate(targetSkill);
            Object.DestroyImmediate(triggerSkill);
            Object.DestroyImmediate(user.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }

        [Test]
        public void BattleSystem_ConsumesBuffAndAppliesMultiplier_WhenMatchingSkillExecuted()
        {
            var user = CreateCharacter("User", true);
            var enemy = CreateCharacter("Enemy", false);
            _battleSystem.StartBattle(new List<CombatCharacter> { user }, new List<CombatCharacter> { enemy });

            // 1. Add buff to user
            var buff = new SkillBoostStatusInstance("special_attack", 50, 3);
            buff.Source = user;
            user.AddStatus(buff);

            // 2. Create the skill that matches
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "special_attack";

            // 3. Execute skill
            _battleSystem.ExecuteSkill(user, skill, new List<CombatCharacter> { enemy });

            // 4. Assert buff is consumed
            Assert.AreEqual(0, user.statusEffects.Count, "Buff should be consumed upon matching skill execution");

            // Since we can't easily retrieve the transient SkillContext here to assert damageMultiplier,
            // the fact that it was removed confirms OnSkillExecute ran and found a match.
            // A more rigorous test would mock or observe CombatCalculator to see if damage multiplier was applied.
            
            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(user.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

        [Test]
        public void BattleSystem_DoesNotConsumeBuff_WhenDifferentSkillExecuted()
        {
            var user = CreateCharacter("User", true);
            var enemy = CreateCharacter("Enemy", false);
            _battleSystem.StartBattle(new List<CombatCharacter> { user }, new List<CombatCharacter> { enemy });

            var buff = new SkillBoostStatusInstance("special_attack", 50, 3);
            buff.Source = user;
            user.AddStatus(buff);

            var diffSkill = ScriptableObject.CreateInstance<SkillData>();
            diffSkill.skillId = "basic_attack";

            _battleSystem.ExecuteSkill(user, diffSkill, new List<CombatCharacter> { enemy });

            Assert.AreEqual(1, user.statusEffects.Count, "Buff should NOT be consumed for a different skill");
            
            Object.DestroyImmediate(diffSkill);
            Object.DestroyImmediate(user.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }
        [Test]
        public void SkillBoostStatusInstance_AppliesCorrectMultiplier()
        {
            var user = CreateCharacter("User", true);
            var enemy = CreateCharacter("Enemy", false);
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.skillId = "special_attack";

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { enemy }, _battleSystem, new System.Random());

            var buff = new SkillBoostStatusInstance("special_attack", 50, 3);
            buff.Host = user;
            
            buff.OnSkillExecute(ctx);

            Assert.AreEqual(1.5f, ctx.damageMultiplier, "Damage multiplier should be 1.5 for amplitude 50");

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(user.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }
    }
}
