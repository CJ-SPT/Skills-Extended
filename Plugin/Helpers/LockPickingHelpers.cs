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
            {"", Plugin.SkillData.LockPickingSkill.DoorPickLevels.GroundZero},
        };

        public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            if (!IsLockPickInInventory())
            {
                owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
                return;
            }

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

        public static bool IsLockPickInInventory()
        {
            return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
                .Where(x => x.TemplateId == "LOCKPICK_PLACEHOLDER").Any();
        }

        public static float CalculateChanceForFailure()
        {
            return 0f;
        }

        public static void ApplyLockPickExperience()
        {
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

                    if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] > 3)
                    {
                        Owner.DisplayPreloaderUiNotification("You broke the lock...");
                        InteractiveObject.KeyId = string.Empty;
                        InteractiveObject.DoorStateChanged(EDoorState.None);
                    }

                    return;
                }

                AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
                LockPickingHelpers.ApplyLockPickExperience();
            }
            else
            {
                Owner.CloseObjectivesPanel();
            }
        }

        /// <summary>
        /// Returns true if the pick attempt succeeded
        /// </summary>
        /// <returns></returns>
        private bool IsAttemptSuccessful(int doorLevel)
        {
            int levelDifference = _skills.Lockpicking.Level - doorLevel;

            // Level difference is to great, never succeed
            if (levelDifference < 0)
            {
                return false;
            }
            // Player level is high enough to always pick this lock
            else if (levelDifference > 5)
            {
                return true;
            }

            float baseSuccessChance = Plugin.SkillData.LockPickingSkill.PickBaseSuccessChance;
            float successMod = Plugin.SkillData.LockPickingSkill.PickBaseDifficultyMod;

            float successChance = baseSuccessChance + (levelDifference * successMod);
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