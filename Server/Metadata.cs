using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SkillsExtended;

public record SeModMetadata : AbstractModMetadata, IModWebMetadata
{
    public override string ModGuid { get; init; } = "com.cj.SkillsExtended";
    public override string Name { get; init; } = "Skills Extended";
    public override string Author { get; init; } = "Cj";
    public override List<string>? Contributors { get; init; } = [];
    public override Version Version { get; init; } = new("2.0.3");
    public override Range SptVersion { get; init; } = new("~4.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/CJ-SPT/Skills-Extended";
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "Attribution-NonCommercial-NoDerivatives 4.0 International";
    
    public static readonly string ResourcesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources");
}

/// <summary>
///     Dummy class used for logging injections inside of razor components.
/// </summary>
[Injectable]
public class SkillsExtendedWebLogger;