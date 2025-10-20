namespace SkillsExtended.Models;

public record ReleaseNote
{
    public required string Version { get; init; }
    public required string CodeName { get; init; }
    public required string ReleaseDate { get; init; }
    public required bool IsLatest { get; init; }
    public List<string>? Features { get; init; }
    public List<string>? Improvements { get; init; }
    public List<string>? BugFixes { get; init; }
}