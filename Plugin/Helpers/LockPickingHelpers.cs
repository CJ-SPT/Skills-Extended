using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using System;
using System.Linq;

namespace SkillsExtended.Helpers
{
    internal static class LockPickingHelpers
    {
        private static SkillManager _skills => Utils.SetActiveSkillManager();
        private static Player _player => Singleton<GameWorld>.Instance.MainPlayer;

        public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
        {
            // Only allow lockpicking if the player is stationary
            if (GetIdleStateType().IsAssignableFrom(owner.Player.CurrentState.GetType()))
            {
                var currentManagedState = owner.Player.CurrentManagedState;
                var lpTime = CalculateTimeForAction();

                owner.ShowObjectivesPanel("Picking lock {0:F1}", lpTime);

                LockPickActionHandler handler = new LockPickActionHandler()
                {
                    Owner = owner,
                    InteractiveObject = interactiveObject,
                };

                Action<bool> action = new Action<bool>(handler.PickLockAction);
                currentManagedState.Plant(true, false, lpTime, action);
            }
            else
            {
                owner.DisplayPreloaderUiNotification("Cannot pick the lock while moving.");
            }
        }

        public static float CalculateChanceForSuccess()
        {
            int level = _skills.Lockpicking.Level;
            bool isElite = _skills.Lockpicking.IsEliteLevel;

            return isElite
                ? (Constants.LP_BASE_CHANCE + (level * Constants.LP_CHANCE_BONUS) + Constants.LP_CHANCE_BONUS_ELITE) * 100f
                : (Constants.LP_BASE_CHANCE + (level * Constants.LP_CHANCE_BONUS)) * 100f;
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
                ? (Constants.LP_BASE_PICK_TIME * (1 - (level * Constants.LP_PICK_TIME_RED) - Constants.LP_CHANCE_BONUS_ELITE))
                : (Constants.LP_BASE_PICK_TIME * (1 - (level * Constants.LP_PICK_TIME_RED)));
        }

        public static void ApplyLockPickExperience()
        {
        }

        public static bool IsLockPickInInventory(string Id)
        {
            return Plugin.Session.Profile.Inventory.EquippedInSlotsTemplateIds.Where(x => x == Id).Any();
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

        public void PickLockAction(bool successful)
        {
            float chanceForSuccess = LockPickingHelpers.CalculateChanceForSuccess();

            if (successful)
            {
                if (chanceForSuccess > UnityEngine.Random.Range(0f, 100f))
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
    }
}