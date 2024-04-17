using Aki.Reflection.Patching;
using AnimationEventSystem;
using System.Collections.Generic;
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

    internal class AnimationEventInitClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(AnimatorControllerStaticData).GetMethod(nameof(AnimatorControllerStaticData.GetEventsByIndex));

        [PatchPostfix]
        private static void Postfix(AnimatorControllerStaticData __instance, List<EventsCollection> ____stateHashToEventsCollection)
        {
        }
    }
}