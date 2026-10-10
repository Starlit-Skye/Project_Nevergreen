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
        
        [Tooltip("Prefabs to summon upon destruction when statusType is LivingFragments.")]
        public System.Collections.Generic.List<GameObject> livingFragmentPrefabs = new System.Collections.Generic.List<GameObject>();

        public void ApplyTo(CombatCharacter character)
        {
            StatusEffectInstance instance;

            // Use specialized subclasses for status types that need custom behavior
            if (statusType == StatusType.Stealth)
            {
                instance = new StealthStatusInstance(duration);
            }
            else if (statusType == StatusType.LivingFragments)
            {
                instance = new LivingFragmentsStatusInstance(null, livingFragmentPrefabs, duration);
            }
            else if (statusType == StatusType.LifeLink)
            {
                instance = new LifeLinkStatusInstance(null, Mathf.RoundToInt(amplitude), duration);
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
