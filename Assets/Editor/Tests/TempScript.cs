using NUnit.Framework;
using UnityEngine;
using Nevergreen.Combat;

namespace Nevergreen.Tests
{
    public class TempScript
    {
        [Test]
        public void RunTestAndPrint()
        {
            var test = new BattleEndTests();
            try {
                test.SetUp();
                test.BattleSystem_SubscribesToDeath_AndEndsBattle();
            } catch (System.Exception ex) {
                Debug.LogError("CAUGHT EXCEPTION BATTLE_END: " + ex.ToString());
            } finally {
                test.TearDown();
            }
        }
    }
}
