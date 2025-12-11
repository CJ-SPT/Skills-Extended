using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class BearRawPowerData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public bool FactionLocked { get; set; }

    [DataMember]
    public float PraporTradingCostDec { get; set; }

    [DataMember]
    public float QuestExpRewardInc { get; set; }

    [DataMember]
    public float AllTraderCostDecrease { get; set; }
}