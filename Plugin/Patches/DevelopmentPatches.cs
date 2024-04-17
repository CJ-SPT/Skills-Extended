using Aki.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class LocationSceneAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(LocationScene).GetMethod(nameof(LocationScene.Awake));

        [PatchPostfix]
        private static void Postfix(LocationScene __instance)
        {
            foreach (var interactableObj in __instance.WorldInteractiveObjects)
            {
                if (interactableObj.KeyId != null && interactableObj.KeyId != string.Empty)
                {
                    Plugin.Log.LogDebug($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {Plugin.Keys.KeyLocale[interactableObj.KeyId]}");
                }
            }
        }
    }
}