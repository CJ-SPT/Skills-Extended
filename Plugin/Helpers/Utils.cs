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

namespace SkillsExtended.Helpers
{
    public static class Utils
    {
        public static Type IdleStateType => _idleStateType;

        private static Type _idleStateType;
        
        // If the player is in the GameWorld, use the main players SkillManager
        public static SkillManager GetActiveSkillManager()
        {
            return Singleton<GameWorld>.Instance?.MainPlayer is not null
                ? Singleton<GameWorld>.Instance?.MainPlayer?.Skills
                : ClientAppUtils.GetMainApp()?.GetClientBackEndSession()?.Profile?.Skills;
        }

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
        
        public static void GetTypes()
        {
            _idleStateType = GetIdleStateType();
        }

        private static Type GetIdleStateType()
        {
            return PatchConstants.EftTypes.Single(x =>
                AccessTools.GetDeclaredMethods(x).Any(method => method.Name == "Plant") &&
                AccessTools.GetDeclaredFields(x).Count >= 5 &&
                x.BaseType.Name == "MovementState");
        }
    }
}