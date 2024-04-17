using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Models;
using System;
using System.Linq;

namespace SkillsExtended.Helpers
{
    internal static class LockPickingHelpers
    {
        private static SkillManager _skills => Utils.GetActiveSkillManager();
        private static Player _player => Singleton<GameWorld>.Instance.MainPlayer;

        private static LockPickingData _lockPicking => Plugin.SkillData.LockPickingSkill;

        public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            if (!IsLockPickInInventory())
            {
                owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
                return;
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

        public static float CalculateChanceForFailure()
        {
            return 0f;
        }

        public static float CalculateTimeForAction()
        {
            int level = _skills.Lockpicking.Level;
            bool isElite = _skills.Lockpicking.IsEliteLevel;

            return isElite
                ? (_lockPicking.BasePickTime * (1 - (level * _lockPicking.PickTimeReduction) - _lockPicking.PickTimeReductionElite))
                : (_lockPicking.BasePickTime * (1 - (level * _lockPicking.PickTimeReduction)));
        }

        public static void ApplyLockPickExperience()
        {
        }

        public static bool IsLockPickInInventory()
        {
            return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).Where(x => x.TemplateId == "LOCKPICK_PLACEHOLDER").Any();
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

        public void PickLockAction(bool successful)
        {
            float chanceForSuccess = CalculateChanceForSuccess();
            float difficulty = UnityEngine.Random.Range(0f, 100f);

            Plugin.Log.LogDebug($"Chance rolled: {chanceForSuccess} Difficulty rolled: {difficulty}");

            if (successful)
            {
                // Random.Range() is placeholder for now
                if (chanceForSuccess > difficulty)
                {
                    AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
                    LockPickingHelpers.ApplyLockPickExperience();
                }
                // TODO: Make this dynamic
                else if (chanceForSuccess < 5f)
                {
                    Owner.DisplayPreloaderUiNotification("You broke the lock...");
                }
                else
                {
                    Owner.DisplayPreloaderUiNotification("Failed to pick the lock...");
                }
            }
            else
            {
                Owner.CloseObjectivesPanel();
            }
        }

        private float CalculateChanceForSuccess()
        {
            int level = _skills.Lockpicking.Level;
            bool isElite = _skills.Lockpicking.IsEliteLevel;

            return isElite
                ? (_lockPicking.BasePickChance + (level * _lockPicking.BonusChancePerLevel) + _lockPicking.BonusChancePerLevelElite) * 100f
                : (_lockPicking.BasePickChance + (level * _lockPicking.BonusChancePerLevel)) * 100f;
        }
    }
}