namespace SkillsExtended.LockPicking;

public class LockPickingEventData
{
    public string DoorId { get; set; }
    public int Attempts { get; set; }
    public bool Unlocked { get; set; }
    public bool Broken { get; set; }
}