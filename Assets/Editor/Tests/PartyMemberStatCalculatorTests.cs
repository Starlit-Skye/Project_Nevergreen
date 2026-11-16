using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Tests
{
    [TestFixture]
    public class PartyMemberStatCalculatorTests
    {
        private List<Object> _objectsToCleanUp;

        [SetUp]
        public void SetUp()
        {
            _objectsToCleanUp = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_objectsToCleanUp != null)
            {
                foreach (var obj in _objectsToCleanUp)
                {
                    if (obj != null && !UnityEditor.EditorUtility.IsPersistent(obj))
                    {
                        Object.DestroyImmediate(obj);
                    }
                }
                _objectsToCleanUp.Clear();
            }
        }

        private PartyMemberInfo CreatePartyMember(int baseMaxHP)
        {
            var charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = "doll_test";
            charData.displayName = "Test Doll";

            var statBlock = ScriptableObject.CreateInstance<StatBlockData>();
            statBlock.maxHP = baseMaxHP;
            _objectsToCleanUp.Add(statBlock);
            charData.statPerLevel.Add(statBlock);
            _objectsToCleanUp.Add(charData);

            return new PartyMemberInfo
            {
                character = charData,
                currentLevel = 1,
                perfections = new List<TraitData>(),
                imperfections = new List<TraitData>(),
                equippedTrinkets = new List<TrinketData>()
            };
        }

        private TraitData CreateStatTrait(string id, TraitType type, StatTarget target, AmplitudeType ampType, int amount)
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.traitId = id;
            trait.displayName = id;
            trait.traitType = type;
            trait.effectStrategies = new List<TraitEffectStrategy>
            {
                new StatModifierTraitStrategy
                {
                    targetStat = target,
                    amplitudeType = ampType,
                    amount = amount
                }
            };
            _objectsToCleanUp.Add(trait);
            return trait;
        }

        private TrinketData CreateStatTrinket(string id, StatTarget target, AmplitudeType ampType, float amount)
        {
            var trinket = ScriptableObject.CreateInstance<TrinketData>();
            trinket.trinketId = id;
            trinket.displayName = id;
            trinket.effectStrategies = new List<TrinketEffectStrategy>
            {
                new StatModifierTrinketStrategy
                {
                    statTarget = target,
                    amplitudeType = ampType,
                    amount = amount
                }
            };
            _objectsToCleanUp.Add(trinket);
            return trinket;
        }

        [Test]
        public void GetEffectiveMaxHP_WithNoTraitsOrTrinkets_ReturnsBaseMaxHP()
        {
            var member = CreatePartyMember(120);

            int maxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);

            Assert.AreEqual(120, maxHP);
        }

        [Test]
        public void GetEffectiveMaxHP_AppliesFlatAndPercentageModifiersFromTraitsAndTrinkets()
        {
            // Base = 100
            // Flat: +20 (Perfection)
            // Percent: +25% (Perfection) - 10% (Imperfection) + 10% (Trinket) = +25%
            // Formula: (100 + 20) * (1 + 0.25) = 120 * 1.25 = 150
            var member = CreatePartyMember(100);
            member.perfections.Add(CreateStatTrait("P_Flat", TraitType.Perfection, StatTarget.MaxHP, AmplitudeType.Flat, 20));
            member.perfections.Add(CreateStatTrait("P_Pct", TraitType.Perfection, StatTarget.MaxHP, AmplitudeType.Percentage, 25));
            member.imperfections.Add(CreateStatTrait("I_Pct", TraitType.Imperfection, StatTarget.MaxHP, AmplitudeType.Percentage, -10));
            member.equippedTrinkets.Add(CreateStatTrinket("T_Pct", StatTarget.MaxHP, AmplitudeType.Percentage, 10f));

            int maxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);

            Assert.AreEqual(150, maxHP);
        }

        [Test]
        public void GetEffectiveMaxHP_ClampsMinimumAtOne()
        {
            var member = CreatePartyMember(50);
            member.imperfections.Add(CreateStatTrait("I_HugeNeg", TraitType.Imperfection, StatTarget.MaxHP, AmplitudeType.Flat, -200));

            int maxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);

            Assert.AreEqual(1, maxHP);
        }

        [Test]
        public void GetEffectiveMaxHP_SafelyIgnoresCombatOnlyStrategiesRequiringOwner()
        {
            var member = CreatePartyMember(100);

            var conditionalTrait = ScriptableObject.CreateInstance<TraitData>();
            conditionalTrait.traitId = "P_Cond";
            conditionalTrait.traitType = TraitType.Perfection;
            conditionalTrait.effectStrategies = new List<TraitEffectStrategy>
            {
                new LowHpStatModifierTraitStrategy
                {
                    targetStat = StatTarget.MaxHP,
                    amplitudeType = AmplitudeType.Percentage,
                    amount = 50
                },
                new RankStatModifierTraitStrategy
                {
                    targetStat = StatTarget.MaxHP,
                    amplitudeType = AmplitudeType.Flat,
                    amount = 50,
                    requiredRank = 1
                }
            };
            _objectsToCleanUp.Add(conditionalTrait);
            member.perfections.Add(conditionalTrait);

            int maxHP = PartyMemberStatCalculator.GetEffectiveMaxHP(member);

            Assert.AreEqual(100, maxHP);
        }
    }
}

