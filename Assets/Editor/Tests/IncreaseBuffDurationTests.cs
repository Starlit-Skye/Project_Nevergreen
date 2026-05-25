using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class IncreaseBuffDurationTests
    {
        private CombatConfig _config;
        private List<GameObject> _cleanup;

        [SetUp]
        public void SetUp()
        {
            _cleanup = new List<GameObject>();
            _config = CombatTestHelper.CreateDefaultConfig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private CombatCharacter CreateTestCharacter(string id, Team team)
        {
            var cc = CombatTestHelper.CreateCombatCharacter(id, team, rank: 1, maxHP: 100, config: _config);
            _cleanup.Add(cc.gameObject);
            return cc;
        }

        [Test]
        public void IncreaseBuffDuration_IncreasesOnlyBuffs()
        {
            var user = CreateTestCharacter("hero", Team.Player);
            var target = CreateTestCharacter("ally", Team.Player);

            // Add a Buff
            var buffInstance = new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 10, 3);
            target.AddStatus(buffInstance);

            // Add a Debuff
            var debuffInstance = new StatusEffectInstance(StatusType.Debuff, StatTarget.Attack, 10, 3);
            target.AddStatus(debuffInstance);

            // Add a DoT (Blight)
            var blightInstance = new StatusEffectInstance(StatusType.Blight, 5, 3);
            target.AddStatus(blightInstance);

            // Create the skill with our effect
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Allies;
            var effect = new IncreaseBuffDurationEffect { durationIncreaseAmount = 2 };
            skill.effects = new List<ISkillEffect> { effect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, new System.Random());

            // Execute effect
            effect.Execute(ctx, target);

            // Assertions
            Assert.AreEqual(5, buffInstance.remainingDuration, "Buff duration should be increased by 2 (from 3 to 5).");
            Assert.AreEqual(3, debuffInstance.remainingDuration, "Debuff duration should NOT be increased.");
            Assert.AreEqual(3, blightInstance.remainingDuration, "Blight duration should NOT be increased.");
        }

        [Test]
        public void IncreaseBuffDuration_DoesNotIncreaseExpiredBuffs()
        {
            var user = CreateTestCharacter("hero", Team.Player);
            var target = CreateTestCharacter("ally", Team.Player);

            // Add a Buff with 0 remaining duration (expired)
            var buffInstance = new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 10, 0);
            target.AddStatus(buffInstance);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Allies;
            var effect = new IncreaseBuffDurationEffect { durationIncreaseAmount = 3 };
            skill.effects = new List<ISkillEffect> { effect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, new System.Random());

            effect.Execute(ctx, target);

            Assert.AreEqual(0, buffInstance.remainingDuration, "Expired buff duration should NOT be increased.");
        }

        [Test]
        public void IncreaseBuffDuration_RespectsHitResolution()
        {
            var user = CreateTestCharacter("hero", Team.Player);
            var target = CreateTestCharacter("ally", Team.Player);

            var buffInstance = new StatusEffectInstance(StatusType.Buff, StatTarget.Speed, 10, 3);
            target.AddStatus(buffInstance);

            // Skill targets Enemies, which makes hit resolution matter
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.targetScope = TargetScope.Enemies;
            var effect = new IncreaseBuffDurationEffect { durationIncreaseAmount = 2 };
            skill.effects = new List<ISkillEffect> { effect };

            var ctx = new SkillContext(user, skill, new List<CombatCharacter> { target }, null, new System.Random());
            
            // Force a miss in the context
            ctx.hasResolvedHit = true;
            ctx.didHit = false;
            ctx.lastResolvedTarget = target;
            ctx.lastResolvedHitIndex = ctx.currentHitIndex;

            effect.Execute(ctx, target);

            Assert.AreEqual(3, buffInstance.remainingDuration, "Buff duration should NOT be increased if the skill missed.");
        }
    }
}
