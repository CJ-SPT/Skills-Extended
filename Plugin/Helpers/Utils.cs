using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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