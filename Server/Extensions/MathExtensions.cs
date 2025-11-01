namespace SkillsExtended.Extensions;

public static class MathExtensions
{
    /// <summary>
    ///     Normalizes a float so that 0.75 = 0.0075 or 0.75%
    /// </summary>
    /// <param name="value">value to normalize</param>
    /// <returns>float as percentage</returns>
    public static float NormalizeToPercentage(this float value)
    {
        return value / 100f;
    }
}