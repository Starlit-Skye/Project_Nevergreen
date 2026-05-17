namespace Nevergreen.Combat
{
    public enum AmplitudeType
    {
        Default,     // Dynamic fallback (Percentage for core stats, Flat for flat/resistance stats)
        Percentage,  // Explicitly treat amplitude as a percentage multiplier of the base stat
        Flat         // Explicitly treat amplitude as a flat addition/subtraction
    }
}
