using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    public class StatusInstanceFactoryTests
    {
        private CombatCharacter _caster;
        private SkillContext _context;
        private BattleSystem _battleSystem;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("BattleSystem");
            _battleSystem = go.AddComponent<BattleSystem>();

            var casterGo = new GameObject("Caster");
            _caster = casterGo.AddComponent<CombatCharacter>();

            var dummySkill = ScriptableObject.CreateInstance<SkillData>();
            dummySkill.modifier = new SkillModifier { damagePercent = 1f };
            _context = new SkillContext(_caster, dummySkill, null, _battleSystem, new System.Random());
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_caster.gameObject);
            Object.DestroyImmediate(_battleSystem.gameObject);
        }

        [Test]
        public void Create_Guard_CreatesGuardInstanceWithCorrectGuardianAndProtectedTarget()
        {
            var allyGo = new GameObject("ProtectedAlly");
            var ally = allyGo.AddComponent<CombatCharacter>();

            var instance = StatusInstanceFactory.Create(
                _context, StatusType.Guard, StatTarget.Speed, 1, 2, AmplitudeType.Default, protectedTarget: ally);
            Assert.IsInstanceOf<GuardStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source, "By default, context.user should be the guardian (Source).");
            Assert.AreEqual(ally, ((GuardStatusInstance)instance).GuardedTarget, "GuardedTarget should match protectedTarget.");

            var otherGuardianGo = new GameObject("OtherGuardian");
            var otherGuardian = otherGuardianGo.AddComponent<CombatCharacter>();
            var instance2 = StatusInstanceFactory.Create(
                _context, StatusType.Guard, StatTarget.Speed, 1, 2, AmplitudeType.Default, guardian: otherGuardian, protectedTarget: ally);

            Assert.IsInstanceOf<GuardStatusInstance>(instance2);
            Assert.AreEqual(otherGuardian, instance2.Source, "Should use explicitly provided guardian.");
            Assert.AreEqual(ally, ((GuardStatusInstance)instance2).GuardedTarget, "GuardedTarget should match protectedTarget.");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("StatusInstanceFactory.*protectedTarget"));
            var invalidInstance = StatusInstanceFactory.Create(
                _context, StatusType.Guard, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsNull(invalidInstance, "Guard without protectedTarget should return null.");

            Assert.AreEqual(_caster, StatusInstanceFactory.ResolveStatusHost(StatusType.Guard, _caster, ally));
            Assert.AreEqual(ally, StatusInstanceFactory.ResolveStatusHost(StatusType.Buff, _caster, ally));

            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(otherGuardianGo);
        }

        [Test]
        public void Create_Flight_CreatesFlightInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.Flight, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsInstanceOf<FlightStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source);
        }

        [Test]
        public void Create_Move_CreatesMoveInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.Move, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsInstanceOf<MoveStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source);
        }

        [Test]
        public void Create_Stealth_CreatesStealthInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.Stealth, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsInstanceOf<StealthStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source);
        }

        [Test]
        public void Create_Shuffle_CreatesShuffleInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.Shuffle, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsInstanceOf<ShuffleStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source);
        }

        [Test]
        public void Create_HealReceivedReduction_CreatesHealReceivedDebuffInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.HealReceivedReduction, StatTarget.Speed, 1, 2, AmplitudeType.Default);
            Assert.IsInstanceOf<HealReceivedDebuffStatusInstance>(instance);
            Assert.AreEqual(_caster, instance.Source);
        }

        [Test]
        public void Create_Default_CreatesBaseInstance()
        {
            var instance = StatusInstanceFactory.Create(_context, StatusType.Buff, StatTarget.Attack, 10, 3, AmplitudeType.Flat);
            Assert.IsInstanceOf<StatusEffectInstance>(instance);
            Assert.IsNotInstanceOf<GuardStatusInstance>(instance);
            Assert.AreEqual(StatTarget.Attack, instance.targetStat);
            Assert.AreEqual(10, instance.amplitude);
            Assert.AreEqual(3, instance.remainingDuration);
            Assert.AreEqual(AmplitudeType.Flat, instance.amplitudeType);
            Assert.AreEqual(_caster, instance.Source);
        }
    }
}
