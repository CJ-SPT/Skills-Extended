// Credits: Drakiaxyz/SPT

using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SkillsExtended.Core;

// Note: We want to run after all mods, to avoid this being lost in mod log
//       spam, so we purposely use MaxValue here

[Injectable(InjectionType.Singleton)]
internal class UpdateChecker(
    ISptLogger<UpdateChecker> logger,
    ConfigController configController
    ) : IOnLoad
{
    public Task OnLoad()
    {
        if (SkillsExtendedInfo.IsBeta)
        {
            return Task.CompletedTask;
        }
        
        if (configController.ServerConfig.CheckForUpdates)
        {
            // Run in a new task so we don't hold the main thread at all, this isn't super critical
            _ = Task.Run(CheckForUpdate);
        }
        
        return Task.CompletedTask;
    }
    
    public bool UpdateAvailable { get; private set; }
    public ReleaseInformation? ReleaseInformation { get; private set; }

    private async Task CheckForUpdate()
    {
        try
        {
            var httpClient = new HttpClient();

            // These headers are _required_ by GitHub API
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CJ-SPT");
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            // TODO: We could probably throw this into a config somewhere, for now hard code it
            var release = await httpClient.GetFromJsonAsync<ReleaseInformation>(
                "https://api.github.com/repos/CJ-SPT/Skills-Extended/releases/latest"
            );
            if (release != null)
            {
                if (release.Version.StartsWith('V'))
                {
                    release.Version = release.Version[1..];
                }
                
                Version latestVersion = new(release.Version);

                var currentVersion = SeModMetadata.Instance.Version;
                Range currentVersionRange = new($"~{currentVersion.Major}.x");

                // First make sure the latest release is in our range, this stops "4.1.0" from being detected as a valid upgrade for "4.0.1"
                if (!currentVersionRange.IsSatisfied(latestVersion))
                {
                    return;
                }

                // Notify the user if an upgrade is available
                if (latestVersion > currentVersion)
                {
                    UpdateAvailable = true;
                    ReleaseInformation = release;
                }
            }
        }
        // We ignore errors, this isn't critical to run, and we don't want to scare users
        catch 
        { }
    }
}

public record ReleaseInformation
{
    [JsonPropertyName("tag_name")]
    public required string Version { get; set; }
    
    [JsonPropertyName("prerelease")]
    public required bool PreRelease { get; set; }

    [JsonPropertyName("html_url")]
    public required string DownloadUrl { get; init; }

    [JsonPropertyName("published_at")]
    public required DateTime ReleaseDate { get; init; }
}