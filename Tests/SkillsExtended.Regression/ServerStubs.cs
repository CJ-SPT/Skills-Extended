using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace SPTarkov.Server.Core.Models.Common
{
    public readonly record struct MongoId(string Value)
    {
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public static MongoId Empty() => default;
    }
}
namespace SPTarkov.Server.Core.Models.Enums
{
    public enum SkillTypes { BearRawpower, UsecNegotiations, Shadowconnections }
    public enum RewardType { Experience, Item }
    public enum BonusType { ScavCooldownTimer }
    public static class Traders
    {
        public static readonly MongoId PRAPOR = new("prapor"), PEACEKEEPER = new("peacekeeper");
    }
}
namespace SPTarkov.Server.Core.Models.Eft.Common
{
    public class PmcData
    {
        public MongoId? Id;
        public Info Info = new();
        public List<Bonus> Bonuses = [];
    }
    public class Info { public string Side; public double? SavageLockTime; }
    public class Bonus { public BonusType Type; public double? Value; }
}
namespace SPTarkov.Server.Core.Models.Eft.Common.Tables
{
    public class Reward { public RewardType Type; public double? Value; }
    public class BarterScheme { public MongoId Template; public double Count; }
    public class TraderAssort { public Dictionary<string, List<List<BarterScheme>>> BarterScheme = []; }
}
namespace SPTarkov.Server.Core.Models.Eft.Profile
{
    public class SptProfile { public CharacterData CharacterData = new(); public ProfileInfo ProfileInfo = new(); }
    public class CharacterData { public PmcData PmcData = new(); }
    public class ProfileInfo { public MongoId? ProfileId; }
}
namespace SPTarkov.Server.Core.Helpers.Profile
{
    public class ProfileHelper
    {
        public int AwardedExperience;
        public PmcData Profile = new();
        public PmcData GetPmcProfile(MongoId id) => Profile;
        public void AddExperienceToPmc(MongoId id, int amount) => AwardedExperience += amount;
    }
}
namespace SkillsExtended.Core { public class ConfigController { public Config.SkillsConfig SkillsConfig; } }
namespace SkillsExtended.Utils
{
    public class SkillUtil
    {
        public Dictionary<SkillTypes, int> Levels = [];
        public int Lookups;
        public bool TryGetSkillLevel(MongoId id, SkillTypes type, out int level) { Lookups++; return Levels.TryGetValue(type, out level); }
        public bool IsEliteLevel(MongoId id, SkillTypes type) => TryGetSkillLevel(id, type, out var level) && level == 51;
    }
}
namespace SPTarkov.Server.Core.Helpers.Commerce
{
    public class RewardHelper { public void ApplyRewards() { } }
    public class PaymentHelper { public bool IsMoneyTpl(MongoId tpl) => tpl.Value == "money"; }
}
namespace SPTarkov.Server.Core.Helpers.Quest { public class QuestRewardHelper { public void GetQuestMoneyRewardBonusMultiplier() { } } }
namespace SPTarkov.Server.Core.Helpers.Traders { public class TraderAssortHelper { public void GetAssort() { } } }
namespace SPTarkov.Server.Core.Generators.Bot { public class PlayerScavGenerator { public void SetScavCooldownTimer() { } } }
namespace SPTarkov.Server.Core.Services.Hideout { public class CircleOfCultistService { public void StartSacrifice() { } public void GetCircleCraftingInfo() { } } }
namespace SPTarkov.Server.Core.Models.Spt.Hideout { public class CircleCraftDetails { public long Time; } }
namespace SPTarkov.Server.Core.Utils { public class TimeUtil { public long GetTimeStamp() => 1000; } }
namespace SPTarkov.Server.Core.Services.Commerce
{
    public class FenceService { public FenceInfo GetFenceInfo(PmcData data) => new(); }
    public class FenceInfo { public double SavageCooldownModifier = 1; }
}
namespace SPTarkov.Server.Core.Models.Spt.Tables
{
    public class GlobalTable { public GlobalConfiguration Configuration = new(); }
    public class GlobalConfiguration { public double SavagePlayCooldown = 1200; }
}
