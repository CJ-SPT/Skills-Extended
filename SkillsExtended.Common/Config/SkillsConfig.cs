using SkillsExtended.Config.Skills;

namespace SkillsExtended.Config;

public class SkillsConfig
{
    public FirstAidData FirstAid { get; set; }
    public FieldMedicineData FieldMedicine { get; set; }
    public WeaponSkillData NatoWeapons { get; set; }
    public WeaponSkillData EasternWeapons { get; set; }
    public LockPickingData LockPicking { get; set; }
    public ProneMovementData ProneMovement { get; set; }
    public SilentOpsData SilentOps { get; set; }
    public EnduranceData Endurance { get; set; }
    public StrengthData Strength { get; set; }
    public VitalityData Vitality { get; set; }
    public HealthData Health { get; set; }
    public MetabolismData Metabolism { get; set; }
    public StressResistanceData StressResistance { get; set; }
    public ImmunityData Immunity { get; set; }
    public ShadowConnectionsData ShadowConnections { get; set; }
    public BearRawPowerData BearRawPower { get; set; }
    public UsecNegotiationsData UsecNegotiations { get; set; }
}