using SPT.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Skills.LockPicking.Patches;

internal class LocationSceneAwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
        => typeof(LocationScene).GetMethod(nameof(LocationScene.Awake));

    [PatchPostfix]
    private static void Postfix(LocationScene __instance)
    {
        foreach (var interactableObj in __instance.WorldInteractiveObjects)
        {
            if (interactableObj.KeyId is null && interactableObj.KeyId != string.Empty) return;
            
            if (SkillsPlugin.Keys.KeyLocale.TryGetValue(interactableObj.KeyId, out var name))
            {
                SkillsPlugin.Log.LogDebug($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {name}");
            }
                
            SkillsPlugin.Log.LogError($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key locale missing...");
        }
    }
}
