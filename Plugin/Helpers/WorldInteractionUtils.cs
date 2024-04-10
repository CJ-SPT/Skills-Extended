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

            ActionsTypesClass action = new ActionsTypesClass();

            action.Name = "Pick lock";
            action.Disabled = !interactiveObject.Operatable; // TODO: Disable if LP level is not high enough for this door

            Interaction keyInfoAction = new Interaction(interactiveObject, owner);

            action.Action = new Action(keyInfoAction.TryPickLock);
            actionReturn.Actions.Add(action);
        }

        public sealed class Interaction
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public Interaction()
            { }

            public Interaction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
            {
                if (interactiveObject == null)
                {
                    throw new ArgumentNullException("Interactive Object is Null...");
                }

                if (owner == null)
                {
                    throw new ArgumentNullException("Owner is null...");
                }

                this.interactiveObject = interactiveObject;
                this.owner = owner;
            }

            public void TryPickLock()
            {
                if (Constants.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
                {
                    NotificationManagerClass.DisplayMessageNotification($"Key for door is {Constants.Keys.KeyLocale[interactiveObject.KeyId]}");
                }

                LockPickingHelpers.PickLock(interactiveObject, owner);
            }
        }
    }
}