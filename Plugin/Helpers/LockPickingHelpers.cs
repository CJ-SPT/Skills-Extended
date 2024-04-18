using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Helpers
{
    internal static class LockPickingHelpers
    {
        public static Dictionary<string, int> DoorAttempts = [];

        private static SkillManager _skills => Utils.GetActiveSkillManager();
        private static Player _player => Singleton<GameWorld>.Instance.MainPlayer;

        private static LockPickingData _lockPicking => Plugin.SkillData.LockPickingSkill;

        private static readonly Dictionary<string, Dictionary<string, int>> LocationDoorIdLevels = new()
        {
            {"factory4_day", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
            {"factory4_night", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
            {"Woods", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Woods},
            {"bigmap", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Customs},
            {"Interchange", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Interchange},
            {"RezervBase", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Reserve},
            {"Shoreline", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Shoreline},
            {"laboratory", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Labs},
            {"lighthouse", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Lighthouse},
            {"TarkovStreets", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Streets},
            {"sandbox", Plugin.SkillData.LockPickingSkill.DoorPickLevels.GroundZero},
        };

        public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            // Check if a lock pick exists in the inventory
            if (!GetLockPicksInInventory().Any())
            {
                owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
                return;
            }

            // Check if the locks broken
            if (DoorAttempts.ContainsKey(interactiveObject.Id))
            {
                if (DoorAttempts[interactiveObject.Id] > 3)
                {
                    owner.DisplayPreloaderUiNotification("You cannot pick a broken lock...");
                    return;
                }
            }

            // Only allow lockpicking if the player is stationary
            if (GetIdleStateType().IsAssignableFrom(owner.Player.CurrentState.GetType()))
            {
                var currentManagedState = owner.Player.CurrentManagedState;
                var lpTime = CalculateTimeForAction();

                owner.ShowObjectivesPanel("Picking lock {0:F1}", lpTime);

                LockPickActionHandler handler = new()
                {
                    Owner = owner,
                    InteractiveObject = interactiveObject,
                };

                Action<bool> action = new(handler.PickLockAction);
                currentManagedState.Plant(true, false, lpTime, action);
            }
            else
            {
                owner.DisplayPreloaderUiNotification("Cannot pick the lock while moving.");
            }
        }

        public static int GetLevelForDoor(string locationId, string doorId)
        {
            return GetDoorLevelsForLocation(locationId)[doorId];
        }

        public static IEnumerable<Item> GetLockPicksInInventory()
        {
            return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
                .Where(x => x.TemplateId == "LOCKPICK_PLACEHOLDER");
        }

        public static void ApplyLockPickActionXp(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, bool isInspect = false, bool IsFailure = false)
        {
            var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            bool xpExists = Plugin.SkillData.LockPickingSkill.XpTable.TryGetValue(doorLevel.ToString(), out float xp);

            if (xpExists)
            {
                var xpToApply = isInspect
                    ? xp * (Plugin.SkillData.LockPickingSkill.InspectLockXpRatio)
                    : xp;

                // Failures recieve 25% xp
                xpToApply = IsFailure
                    ? xpToApply * 0.25f
                    : xpToApply;

                Plugin.Log.LogInfo($"Lockpicking xp found in table : {xpToApply} experience for door level {doorLevel} : IsInspect {isInspect} : IsFailure {IsFailure}");

                _skills.Lockpicking.Current += xpToApply;

                return;
            }

            Plugin.Log.LogWarning($"Lockpicking xp not found in table.. defaulting to {6f} experience");
            _skills.Lockpicking.Current += 6f;
        }

        private static float CalculateTimeForAction()
        {
            int level = _skills.Lockpicking.Level;
            bool isElite = _skills.Lockpicking.IsEliteLevel;

            return isElite
                ? (_lockPicking.PickBaseTime * (1 - (level * _lockPicking.PickTimeReductionElite)))
                : (_lockPicking.PickBaseTime * (1 - (level * _lockPicking.PickTimeReduction)));
        }

        private static Dictionary<string, int> GetDoorLevelsForLocation(string locationId)
        {
            return LocationDoorIdLevels[locationId];
        }

        private static Type GetIdleStateType()
        {
            return PatchConstants.EftTypes.Single(x =>
                AccessTools.GetDeclaredMethods(x).Any(method => method.Name == "Plant") &&
                AccessTools.GetDeclaredFields(x).Count >= 5 &&
                x.BaseType.Name == "MovementState");
        }
    }

    internal class LockPickActionHandler
    {
        public GamePlayerOwner Owner;
        public WorldInteractiveObject InteractiveObject;

        private static SkillManager _skills => Utils.GetActiveSkillManager();

        private static LockPickingData _lockPicking => Plugin.SkillData.LockPickingSkill;

        public void PickLockAction(bool actionCompleted)
        {
            int doorLevel = LockPickingHelpers.GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

            // If the player completed the full timer uninterrupted
            if (actionCompleted)
            {
                // Attempt was not successful
                if (!IsAttemptSuccessful(doorLevel))
                {
                    Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

                    // Add to the counter
                    if (!LockPickingHelpers.DoorAttempts.ContainsKey(InteractiveObject.Id))
                    {
                        LockPickingHelpers.DoorAttempts.Add(InteractiveObject.Id, 1);
                    }
                    else
                    {
                        LockPickingHelpers.DoorAttempts[InteractiveObject.Id]++;
                    }

                    // Break the lock if more than 3 failed attempts
                    if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] > 3)
                    {
                        Owner.DisplayPreloaderUiNotification("You broke the lock...");
                        InteractiveObject.KeyId = string.Empty;
                        InteractiveObject.Operatable = false;
                        InteractiveObject.DoorStateChanged(EDoorState.None);
                    }

                    // Apply failure xp
                    LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, false, true);
                    RemoveUseFromLockpick();

                    return;
                }

                RemoveUseFromLockpick();
                LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
                AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
            }
            else
            {
                Owner.CloseObjectivesPanel();
            }
        }

        private void RemoveUseFromLockpick()
        {
            // Remove a use from a lockpick in the inventory
            var lockPicks = LockPickingHelpers.GetLockPicksInInventory();
            Item lockpick = lockPicks.First();

            if (lockpick is GClass2720 pick)
            {
                pick.KeyComponent.NumberOfUsages++;

                // lockpick has no uses left, destroy it
                if (pick.KeyComponent.NumberOfUsages >= pick.KeyComponent.Template.MaximumNumberOfUsage && pick.KeyComponent.Template.MaximumNumberOfUsage > 0)
                {
                    InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Owner.Player);

                    inventoryController.DestroyItem(lockpick);
                }
            }
        }

        /// <summary>
        /// Returns true if the pick attempt succeeded
        /// </summary>
        /// <returns></returns>
        private bool IsAttemptSuccessful(int doorLevel)
        {
            int levelDifference = _skills.Lockpicking.Level - doorLevel;

            // Player level is high enough to always pick this lock
            if (levelDifference > 10)
            {
                Plugin.Log.LogDebug("Pick attempt success chance: Player out leveled this lock: SUCCEED ");
                return true;
            }

            float baseSuccessChance = _lockPicking.PickBaseSuccessChance;
            float successMod = _lockPicking.PickBaseDifficultyMod;

            // Never below 0, never above 100
            float successChance = UnityEngine.Mathf.Clamp(baseSuccessChance + (levelDifference * successMod), 0f, 100f);
            float roll = UnityEngine.Random.Range(0f, 100f);

            Plugin.Log.LogDebug($"Pick attempt success chance: {successChance}, Roll: {roll}");

            if (successChance > roll)
            {
                return true;
            }

            return false;
        }
    }
}