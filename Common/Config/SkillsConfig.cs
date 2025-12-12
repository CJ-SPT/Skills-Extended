using System.Runtime.Serialization;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.Config;

[DataContract]
public class SkillsConfig
{
    [DataMember]
    public FirstAidData FirstAid { get; set; }
    
    [DataMember]
    public FieldMedicineData FieldMedicine { get; set; }
    
    [DataMember]
    public WeaponSkillData NatoWeapons { get; set; }
    
    [DataMember]
    public WeaponSkillData EasternWeapons { get; set; }
    
    [DataMember]
    public LockPickingData LockPicking { get; set; }
    
    [DataMember]
    public ProneMovementData ProneMovement { get; set; }
    
    [DataMember]
    public SilentOpsData SilentOps { get; set; }
    
    [DataMember]
    public EnduranceData Endurance { get; set; }
    
    [DataMember]
    public StrengthData Strength { get; set; }
    
    [DataMember]
    public VitalityData Vitality { get; set; }
    
    [DataMember]
    public HealthData Health { get; set; }
    
    [DataMember]
    public MetabolismData Metabolism { get; set; }
    
    [DataMember]
    public StressResistanceData StressResistance { get; set; }
    
    [DataMember]
    public ImmunityData Immunity { get; set; }
    
    [DataMember]
    public ShadowConnectionsData ShadowConnections { get; set; }
    
    [DataMember]
    public BearRawPowerData BearRawPower { get; set; }
    
    [DataMember]
    public UsecNegotiationsData UsecNegotiations { get; set; }
}