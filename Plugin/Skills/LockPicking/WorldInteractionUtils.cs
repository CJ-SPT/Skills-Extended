using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Linq;
using SkillsExtended.Skills.LockPicking.Actions;

namespace SkillsExtended.Skills.LockPicking;

public static class WorldInteractionUtils
{
    public static bool IsBotInteraction(GamePlayerOwner owner)
    {
        if (owner is null)
        {
            throw new ArgumentNullException("owner is null...");
        }

        return owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id;
    }

    public static void AddLockpickingInteraction(this WorldInteractiveObject interactiveObject, ActionsReturnClass actionReturn, GamePlayerOwner owner)
    {
        LockPickingInteraction lockPickInteraction = new(interactiveObject, owner);

        if (!IsDoorValidForLockPicking(interactiveObject))
        {
            // Secondary check to prevent action showing on open or closed doors that have
            // already been picked.
            if (interactiveObject.DoorState == EDoorState.Open || interactiveObject.DoorState == EDoorState.Shut)
            {
                return;
            }

            ActionsTypesClass notValidAction = new()
            {
                Name = "Door cannot be opened",
                Disabled = interactiveObject.Operatable
            };

            notValidAction.Action = lockPickInteraction.DoorNotValid;
            actionReturn.Actions.Add(notValidAction);

            return;
        }

        ActionsTypesClass validAction = new()
        {
            Name = "Pick lock",
            Disabled = !interactiveObject.Operatable && !LpHelpers.GetLockPicksInInventory().Any()
        };

        validAction.Action = lockPickInteraction.TryPickLock;
        actionReturn.Actions.Add(validAction);
    }

    public static void AddKeyCardInteraction(this KeycardDoor door, ActionsReturnClass actionReturn, GamePlayerOwner owner)
    {
        HackTerminalInteraction hackTerminalOperation = new(door, owner);

        if (!IsDoorValidForLockPicking(door))
        {
            // Secondary check to prevent action showing on open or closed doors that have
            // already been picked.
            if (door.DoorState == EDoorState.Open || door.DoorState == EDoorState.Shut)
            {
                return;
            }

            ActionsTypesClass notValidAction = new()
            {
                Name = "Door cannot be opened",
                Disabled = door.Operatable
            };

            notValidAction.Action = hackTerminalOperation.DoorNotValid;
            actionReturn.Actions.Add(notValidAction);

            return;
        }

        ActionsTypesClass validAction = new()
        {
            Name = "Hack terminal",
            Disabled = !door.Operatable && !LpHelpers.IsFlipperZeroInInventory()
        };

        validAction.Action = hackTerminalOperation.TryHackTerminal;
        actionReturn.Actions.Add(validAction);
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
            Disabled = !interactiveObject.Operatable
        };

        LockInspectInteraction keyInfoAction = new(interactiveObject, owner);

        action.Action = keyInfoAction.TryInspectLock;
        actionReturn.Actions.Add(action);
    }
    
    private static bool IsDoorValidForLockPicking(WorldInteractiveObject interactiveObject)
    {
        if (interactiveObject.DoorState != EDoorState.Locked || !interactiveObject.Operatable || !Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
        {
            return false;
        }

        return true;
    }

    private static bool IsValidDoorForInspect(WorldInteractiveObject interactiveObject)
    {
        if (interactiveObject.KeyId is null || interactiveObject.KeyId == string.Empty
            || !interactiveObject.Operatable || interactiveObject.DoorState != EDoorState.Locked
            || !Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
        {
            return false;
        }

        return true;
    }

    private sealed class LockPickingInteraction
    {
        private GamePlayerOwner owner;
        private WorldInteractiveObject interactiveObject;

        public LockPickingInteraction()
        { }

        public LockPickingInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
            this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
        }

        public void TryPickLock()
        {
            LockPickActions.PickLock(interactiveObject, owner);
        }

        public void DoorNotValid()
        {
            owner.DisplayPreloaderUiNotification("This door is cannot be opened.");
        }
    }

    private sealed class HackTerminalInteraction
    {
        private GamePlayerOwner owner;
        private KeycardDoor door;

        public HackTerminalInteraction()
        { }

        public HackTerminalInteraction(KeycardDoor door, GamePlayerOwner owner)
        {
            this.door = door ?? throw new ArgumentNullException("keycard door is Null...");
            this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
        }

        public void TryHackTerminal()
        {
            LockPickActions.HackTerminal(door, owner);
        }

        public void DoorNotValid()
        {
            owner.DisplayPreloaderUiNotification("This door is cannot be opened.");
        }
    }
    
    private sealed class LockInspectInteraction
    {
        private GamePlayerOwner owner;
        private WorldInteractiveObject interactiveObject;

        public LockInspectInteraction()
        { }

        public LockInspectInteraction(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            this.interactiveObject = interactiveObject ?? throw new ArgumentNullException("Interactive Object is Null...");
            this.owner = owner ?? throw new ArgumentNullException("Owner is null...");
        }

        public void TryInspectLock()
        {
            if (Plugin.Keys.KeyLocale.ContainsKey(interactiveObject.KeyId))
            {
                InspectLockActionHandler handler = new()
                {
                    Owner = owner,
                    InteractiveObject = interactiveObject,
                };
                
                Action action = new(handler.InspectLockAction);
                
                LockPickActions.InspectDoor(interactiveObject, owner, action);
                return;
            }
            
            Plugin.Log.LogError($"Missing locale data for door {interactiveObject.Id} and key {interactiveObject.KeyId}");
        }
    }
}
