namespace SkillsExtended.Models;

public record KeysResponse
{
    public required Dictionary<string, string> KeyLocale { get; set; }
}