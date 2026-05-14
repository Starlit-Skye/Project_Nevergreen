using UnityEngine;

namespace Nevergreen.Combat.AI
{
    /// <summary>
    /// The runtime component responsible for evaluating AI profiles and making decisions.
    /// </summary>
    [RequireComponent(typeof(CombatCharacter))]
    public class AIBrain : MonoBehaviour
    {
        [Tooltip("The AI profile containing the logic to execute.")]
        public EnemyAIProfile profile;

        /// <summary>
        /// The turn history for this specific instance.
        /// </summary>
        public AIHistory History { get; private set; } = new AIHistory();

        private CombatCharacter _self;

        /// <summary>
        /// The combat character this brain controls.
        /// </summary>
        public CombatCharacter Self 
        { 
            get 
            {
                if (_self == null) _self = GetComponent<CombatCharacter>();
                return _self;
            }
        }

        /// <summary>
        /// Evaluates the combat state and returns a decision based on the assigned AI Profile.
        /// </summary>
        public AIDecision EvaluateTurn(BattleSystem battleContext)
        {
            if (profile == null || profile.behaviors == null || profile.behaviors.Count == 0)
            {
                Debug.LogWarning($"[AIBrain] No valid AI profile or behaviors set on {gameObject.name}. Passing turn.");
                return AIDecision.Pass;
            }

            foreach (var behavior in profile.behaviors)
            {
                if (behavior == null) continue;

                if (behavior.TryGetDecision(this, battleContext, out AIDecision decision))
                {
                    return decision;
                }
            }

            // Fallback if all behaviors fail to produce a decision
            Debug.LogWarning($"[AIBrain] All AI behaviors failed on {gameObject.name}. Passing turn.");
            return AIDecision.Pass;
        }

        /// <summary>
        /// Records the decision in history after the combat system has accepted and executed it.
        /// </summary>
        public void RecordDecision(AIDecision decision)
        {
            History.RecordDecision(decision);
        }
    }
}
