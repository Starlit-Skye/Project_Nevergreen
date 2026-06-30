using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    public class StatusEffectOnSpawn : MonoBehaviour
    {
        public StatusType statusType;
        public int duration = 1;
        public float amplitude = 0f;
        public AmplitudeType amplitudeType = AmplitudeType.Default;
        public StatTarget targetStat = StatTarget.MaxHP;

        public void ApplyTo(CombatCharacter character)
        {
            StatusEffectInstance instance;

            // Use specialized subclasses for status types that need custom behavior
            if (statusType == StatusType.Stealth)
            {
                instance = new StealthStatusInstance(duration);
            }
            else
            {
                instance = new StatusEffectInstance(
                    statusType,
                    targetStat,
                    Mathf.RoundToInt(amplitude),
                    duration,
                    amplitudeType
                );
            }

            character.AddStatus(instance);
        }
    }
}
