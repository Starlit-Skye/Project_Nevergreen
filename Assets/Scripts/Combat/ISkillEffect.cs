using Nevergreen.Data;

namespace Nevergreen.Combat
{
    /// <summary>
    /// The base interface for all modular skill effects in combat.
    /// Classes implementing this should be marked [System.Serializable] to work with [SerializeReference] in SkillData.
    /// </summary>
    public interface ISkillEffect
    {
        void Execute(SkillContext context, CombatCharacter target);
    }
}
