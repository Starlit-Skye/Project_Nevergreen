using System;
using UnityEngine;
using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// Specialized status instance for the Stealth effect.
    /// Breaks when the host executes an offensive skill (targetScope == TargetScope.Enemies).
    /// </summary>
    [Serializable]
    public class StealthStatusInstance : StatusEffectInstance
    {
        public StealthStatusInstance(int duration) 
            : base(StatusType.Stealth, 0, duration)
        {
        }
    }
}
