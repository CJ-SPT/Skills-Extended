using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SkillsExtended;

public record ModMetadata : IModMetadata, IModBlazorMetadata
{
    public static ModMetadata Instance { get; } = new();
    public Version Version { get; init; } = new(SkillsExtendedInfo.VERSION);
    public Range SptVersion { get; init; } = new(SkillsExtendedInfo.SPT_VERSION);
    public bool HasPrepatcher { get; init; }

    public string ModGuid { get; init; } = SkillsExtendedInfo.MOD_GUID;
    public string Name { get; init; } = "Skills Extended";
    public string Author { get; init; } = "Cj";
    public List<string>? Contributors { get; init; } = [];
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/CJ-SPT/Skills-Extended";
    public string License { get; init; } =
        "Attribution-NonCommercial-NoDerivatives 4.0 International";

    public static readonly string ResourcesDirectory = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "Resources"
    );

    public string? WWWRootUrl { get; init; } = "skills-extended";
    public string? HomePage { get; init; } = "/skills-extended";
    public string? HomePageDescription { get; init; } = "Web interface for skills extended";
}
