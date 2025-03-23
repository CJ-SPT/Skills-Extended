using Newtonsoft.Json;
using SPT.Common.Http;
using System;

namespace SkillsExtended.Helpers;

public static class Utils
{
    // Get Json from the server
    public static T Get<T>(string url)
    {
        var req = RequestHandler.GetJson(url);
            
        if (string.IsNullOrEmpty(req))
        {
            throw new InvalidOperationException("The response from the server is null or empty.");
        }

        return JsonConvert.DeserializeObject<T>(req);
    }
    
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
    
    public static int RandomizePercentage(int value, double percentage = 0.10)
    {
        if (percentage is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 1.");
        }

        var variation = value * percentage;
        var min = (int)Math.Floor(value - variation);
        var max = (int)Math.Ceiling(value + variation);

        // Ensure min is not greater than max.
        // +1 because Next() is exclusive on max
        if (min <= max) return UnityEngine.Random.Range(min, max + 1); 
        
        (min, max) = (max, min);

        // +1 because Next() is exclusive on max

        ;
        return UnityEngine.Random.Range(min, max + 1); 
    }
}