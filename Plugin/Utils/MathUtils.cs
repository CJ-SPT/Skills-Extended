using System;

namespace SkillsExtended.Utils;

public static class MathUtils
{
    public static float RandomizePercentage(float value, float percentage = 0.10f)
    {
        if (percentage is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 1.");
        }

        var variation = value * percentage;
        var min = value - variation;
        var max = value + variation;

        return UnityEngine.Random.Range(0f, 1f) * (max - min) + min;
    }
    
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