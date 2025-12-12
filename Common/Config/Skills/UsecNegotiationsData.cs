using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class UsecNegotiationsData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public bool FactionLocked { get; set; }

    [DataMember]
    public float PeacekeeperTradingCostDec { get; set; }

    [DataMember]
    public float QuestMoneyRewardInc { get; set; }

    [DataMember]
    public float AllTraderCostDecrease { get; set; }
}