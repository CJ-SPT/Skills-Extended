using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Config;
using SkillsExtended.Skills.FirstAid.Patches;
using SkillsExtended.Skills.SilentOps.Patches;
using SkillsExtended.Utils;

namespace SkillsExtended;

internal static class FirstAidSpeedChecks
{
    public static void Run(SkillsConfig config, Action<bool, string> check)
    {
        var firstAidEnabled = config.FirstAid.Enabled;
        var silentOpsEnabled = config.SilentOps.Enabled;
        try
        {
            config.FirstAid.Enabled = true;
            config.SilentOps.Enabled = true;
            GameUtils.SkillManager = null; // Dedicated host: no local skills.
            var component = new HealthEffectsComponent();
            var controller = new Player.MedsController { Item = component.Item };

            // Repeated missing-owner calls must remain safe, including after a
            // first call would have set the old one-time logging flag.
            foreach (var owner in new object[]
            {
                null, new object(),
                new InventoryController { Profile = null },
                new InventoryController(),
                new InventoryController { Profile = new() { SkillsInfo = new SkillManager() } },
            })
            {
                component.Item.Owner = owner;
                for (var i = 0; i < 3; i++)
                {
                    AssertMedical(10f, 2f, "missing owner skills retain native medical timing");
                }
            }

            var ownerSkills = new SkillManager();
            ownerSkills.SkillsExtendedManager = new(ownerSkills, config);
            ownerSkills.SkillsExtendedManager.FirstAidItemSpeedBuff.Value = 0.25f;
            component.Item.Owner = new InventoryController
            {
                Profile = new() { SkillsInfo = ownerSkills },
            };
            AssertMedical(7.5f, 2.5f, "headless treatment uses the item owner's bonus");

            // Another actor's local skills must not leak into treatment timing.
            GameUtils.SkillManager = new SkillManager();
            GameUtils.SkillManager.SkillsExtendedManager = new(GameUtils.SkillManager, config);
            GameUtils.SkillManager.SkillsExtendedManager.FirstAidItemSpeedBuff.Value = 0.5f;
            AssertMedical(7.5f, 2.5f, "medical timing ignores other local player skills");
            config.FirstAid.Enabled = false;
            AssertMedical(10f, 2f, "disabled First Aid retains native timing");

            var melee = typeof(MeleeSpeedPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var skills in new[] { null, new SkillManager() })
            {
                GameUtils.SkillManager = skills;
                for (var i = 0; i < 3; i++)
                    AssertMelee(2f, "missing local skills retain native melee speed");
            }
            GameUtils.SkillManager = ownerSkills;
            ownerSkills.SkillsExtendedManager.SilentOpsIncMeleeSpeedBuff.Value = 0.2f;
            AssertMelee(2.4f, "local melee bonus still applies");
            config.SilentOps.Enabled = false;
            AssertMelee(2f, "disabled Silent Ops retains native speed");

            void AssertMelee(float expected, string label)
            {
                object[] args = [2f];
                melee.Invoke(null, args);
                check(Math.Abs((float)args[0] - expected) < 0.00001f, label);
            }

            void AssertMedical(float expectedTime, float expectedSpeed, string label)
            {
                var time = 10f;
                var speed = 2f;
                HealthEffectUseTimePatch.PostFix(ref time, component);
                SpawnPatch.PreFix(controller, ref speed);
                check(Math.Abs(time - expectedTime) < 0.00001f, label + " use time");
                check(Math.Abs(speed - expectedSpeed) < 0.00001f, label + " animation speed");
            }
        }
        finally
        {
            config.FirstAid.Enabled = firstAidEnabled;
            config.SilentOps.Enabled = silentOpsEnabled;
            GameUtils.SkillManager = null;
        }
    }
}
