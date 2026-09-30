namespace SkillsExtended.Signals;

public static class SignalsMaps
{
    // GameWorld/Fika use display-case IDs (e.g. Woods); SPT configuration uses woods.
    public static string Normalize(string map) => map?.Trim().ToLowerInvariant() ?? "";

    public static bool IsSupported(string map) => Normalize(map) is "woods" or "bigmap";

    public static bool Same(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) && Normalize(a) == Normalize(b);
}
