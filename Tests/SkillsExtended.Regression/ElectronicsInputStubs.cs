namespace EFT;

public static class GamePlayerOwner
{
    public static Player? MyPlayer { get; set; }
    public static bool IgnoreInputWithKeepResetLook { get; set; }
    public static bool IgnoreInputInNPCDialog { get; set; }
}
