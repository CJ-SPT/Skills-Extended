using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;

namespace SkillsExtended.Helpers
{
    public static class WorldInteractionUtils
    {
        public static bool IsDoorValidForLockPicking(WorldInteractiveObject interactiveObject)
        {
            if (!interactiveObject.Operatable)
            {
                return false;
            }

            if (interactiveObject.DoorState != EDoorState.Locked)
            {
                return false;
            }

            return true;
        }

        public static bool IsBotInteraction(GamePlayerOwner owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner is null...");
            }

            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return true;
            }

            return false;
        }

        public static void AddGetKeyIdToActionList(this WorldInteractiveObject interactiveObject, ActionsReturnClass actionReturn, GamePlayerOwner owner)
        {
            if (interactiveObject.DoorState != EDoorState.Locked || interactiveObject.KeyId == "")
            {
                return;
            }

            ActionsTypesClass action = new()
            {
                Name = "Pick lock",
                Disabled = !interactiveObject.Operatable && !LockPickingHelpers.IsLockPickInInventory()
            };

            LockPickingInteraction keyInfoAction = new(interactiveObject, owner);

            action.Action = new Action(keyInfoAction.TryPickLock);
            actionReturn.Actions.Add(action);
        }

        public sealed class LockPickingInteraction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public LockPickingInteraction()
            { }

            public LockPickingInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
                this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
            }

            public void TryPickLock()
            {
                if (Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
                {
                    NotificationManagerClass.DisplayMessageNotification($"Key for door is {Plugin.Keys.KeyLocale[interactiveObject.KeyId]}");
                }

                int level = LockPickingHelpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

                if (owner.Player.Skills.Lockpicking.Level < level)
                {
                    owner.DisplayPreloaderUiNotification("This lock is hard for your level...");
                }

                LockPickingHelpers.PickLock(interactiveObject, owner);
            }
        }
    }
}