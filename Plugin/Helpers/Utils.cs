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
}