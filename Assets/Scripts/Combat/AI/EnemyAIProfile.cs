using System.Collections.Generic;
using UnityEngine;
using Nevergreen.Combat.AI.Nodes;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// The data asset that defines the "Personality" and rules for an enemy.
    /// Contains a prioritized list of behaviors evaluated top-to-bottom.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyAIProfile", menuName = "Nevergreen/AI/Enemy AI Profile")]
    public class EnemyAIProfile : ScriptableObject
    {
        [Tooltip("The list of behaviors the AI will evaluate in order. The first one that succeeds will be executed.")]
        [SerializeReference]
        [Nevergreen.Attributes.SubclassSelector]
        public List<AIBehaviorNode> behaviors = new List<AIBehaviorNode>();
    }
}