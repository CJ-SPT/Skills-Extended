using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Helpers
{
    public static class WorldInteractionUtils
    {
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

        public static void AddLockpickingInteraction(this WorldInteractiveObject interactiveObject, ActionsReturnClass actionReturn, GamePlayerOwner owner)
        {
            if (!IsDoorValidForLockPicking(interactiveObject))
            {
                return;
            }

            ActionsTypesClass action = new()
            {
                Name = "Pick lock",
                Disabled = !interactiveObject.Operatable && !LockPickingHelpers.GetLockPicksInInventory().Any()
            };

            LockPickingInteraction pickLockAction = new(interactiveObject, owner);

            action.Action = new Action(pickLockAction.TryPickLock);
            actionReturn.Actions.Add(action);
        }

        public static void AddInspectInteraction(this WorldInteractiveObject interactiveObject, ActionsReturnClass actionReturn, GamePlayerOwner owner)
        {
            if (!IsValidDoorForInspect(interactiveObject))
            {
                return;
            }

            ActionsTypesClass action = new()
            {
                Name = "Inspect Lock",
                Disabled = !interactiveObject.Operatable || LockInspectInteraction.InspectedDoors.Contains(interactiveObject.Id)
            };

            LockInspectInteraction keyInfoAction = new(interactiveObject, owner);

            action.Action = new Action(keyInfoAction.TryInspectLock);
            actionReturn.Actions.Add(action);
        }

        private static bool IsDoorValidForLockPicking(WorldInteractiveObject interactiveObject)
        {
            if (interactiveObject.DoorState != EDoorState.Locked || !interactiveObject.Operatable)
            {
                return false;
            }

            return true;
        }

        private static bool IsValidDoorForInspect(WorldInteractiveObject interactiveObject)
        {
            if (interactiveObject.KeyId == null || interactiveObject.KeyId == string.Empty || !interactiveObject.Operatable || interactiveObject.DoorState != EDoorState.Locked)
            {
                return false;
            }

            return true;
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
                int level = LockPickingHelpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

                if (owner.Player.Skills.Lockpicking.Level < level)
                {
                    owner.DisplayPreloaderUiNotification("This lock is hard for your level...");
                }

                LockPickingHelpers.PickLock(interactiveObject, owner);
            }
        }

        public sealed class LockInspectInteraction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public static List<string> InspectedDoors = [];

            public LockInspectInteraction()
            { }

            public LockInspectInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
                this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
            }

            public void TryInspectLock()
            {
                if (InspectedDoors.Contains(interactiveObject.Id))
                {
                    return;
                }

                if (Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
                {
                    NotificationManagerClass.DisplayMessageNotification($"Key for door is {Plugin.Keys.KeyLocale[interactiveObject.KeyId]}");

                    InspectedDoors.Add(interactiveObject.Id);

                    LockPickingHelpers.ApplyLockPickActionXp(interactiveObject, owner, true);
                }
            }
        }
    }
}