using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using SkillsExtended.Core;
using SkillsExtended.Core.Editing;
using SkillsExtended.Web.Layouts;
using SkillsExtended.Web.Pages;
using SkillsExtended.Web.Shared;
using SPTarkov.Common.Models.Logging;

public static class RenderingChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, OfflineJs>();
        services.AddSingleton<NavigationManager, OfflineNavigation>();
        services.AddSingleton(
            new ConfigController(
                DispatchProxy.Create<ISptLogger<ConfigController>, SilentLogger>(),
                []
            )
        );
        services.AddSingleton(RewardCatalogChecks.Locales());
        services.AddSingleton(RewardCatalogChecks.Templates());
        var output = Path.Combine(AppContext.BaseDirectory, "rendered");
        Directory.CreateDirectory(output);
        foreach (var skill in SkillCatalog.Navigation)
        {
            await using var provider = services.BuildServiceProvider();
            await using var renderer = new HtmlRenderer(
                provider,
                provider.GetRequiredService<ILoggerFactory>()
            );
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                RenderFragment body = builder =>
                {
                    builder.OpenComponent(0, SkillsExtended.Config.NativeSkillCatalog.ByKey.ContainsKey(skill.Key)
                        ? typeof(NativeSkillEditor) : typeof(SkillEditor));
                    builder.AddAttribute(1, "SkillKey", skill.Key);
                    builder.CloseComponent();
                };
                var root = await renderer.RenderComponentAsync<BaseLayout>(
                    ParameterView.FromDictionary(
                        new Dictionary<string, object?> { ["Body"] = body }
                    )
                );
                return root.ToHtmlString();
            });
            check(
                html.Contains("se-fields")
                    && html.Contains(skill.Name)
                    && !html.Contains("Could not load configuration"),
                $"Render actual {skill.Name} page with its layout"
            );
            if (skill.Key is "LockPicking" or "Hacking" or "SignalsIntelligence")
                check(html.Contains("player guide") && html.Contains("/skills-extended/guides/"),
                    $"{skill.Name} settings link to its public guide");
            await Write(output, skill.Slug, html);
        }

        await using var homeProvider = services.BuildServiceProvider();
        await using var homeRenderer = new HtmlRenderer(
            homeProvider,
            homeProvider.GetRequiredService<ILoggerFactory>()
        );
        var home = await homeRenderer.Dispatcher.InvokeAsync(async () =>
        {
            RenderFragment body = builder =>
            {
                builder.OpenComponent<Home>(0);
                builder.CloseComponent();
            };
            var root = await homeRenderer.RenderComponentAsync<BaseLayout>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Body"] = body })
            );
            return root.ToHtmlString();
        });
        check(
            home.Contains($"{SkillCatalog.Navigation.Count} skills") && home.Contains("se-card-grid"),
            "Render actual overview with all skill cards"
        );
        await Write(output, "overview", home);
        await using var levelingProvider = services.BuildServiceProvider();
        await using var levelingRenderer = new HtmlRenderer(levelingProvider, levelingProvider.GetRequiredService<ILoggerFactory>());
        var levelingHtml = await levelingRenderer.Dispatcher.InvokeAsync(async () =>
        {
            RenderFragment body = builder => { builder.OpenComponent<LevelingSpeed>(0); builder.CloseComponent(); };
            var root = await levelingRenderer.RenderComponentAsync<BaseLayout>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Body"] = body }));
            return root.ToHtmlString();
        });
        check(levelingHtml.Contains("Individual skill leveling speed") && levelingHtml.Contains("Weapon mastery multiplier")
            && levelingHtml.Contains("Signals Intelligence") && levelingHtml.Contains("Hideout Management")
            && !levelingHtml.Contains("BotReload"), "Render all player leveling controls within the real editor layout");
        await Write(output, "leveling-speed", levelingHtml);
        check(home.Contains("Read mini-game guides") && home.Contains("Mini-game guides"),
            "Overview and editor navigation link to the guide hub");
        var accessSession = new EditorSession(
            await homeProvider.GetRequiredService<ConfigController>().GetSnapshotAsync()
        );
        accessSession.Server.AuthorizedEditorProfiles.Add("0123456789abcdef01234567");
        accessSession.Server.AuthorizedEditorProfiles.Add("0123456789abcdef01234569");
        var accessProfiles = new ProfileChoice[]
        {
            new("0123456789abcdef01234567", "Authorized player"),
            new("0123456789abcdef01234568", "Other player"),
        };
        var accessHtml = await homeRenderer.Dispatcher.InvokeAsync(async () =>
        {
            RenderFragment body = builder =>
            {
                builder.OpenComponent<ClientEditorAccess>(0);
                builder.AddAttribute(1, "Profiles", accessProfiles);
                builder.CloseComponent();
            };
            var root = await homeRenderer.RenderComponentAsync<CascadingValue<EditorSession>>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        ["Value"] = accessSession,
                        ["ChildContent"] = body,
                    }
                )
            );
            return root.ToHtmlString();
        });
        check(
            accessHtml.Contains("Search profiles")
                && accessHtml.Contains("Authorized player")
                && accessHtml.Contains("Other player")
                && accessHtml.Contains("Unavailable profile")
                && accessHtml.Contains("checked")
                && accessHtml.Contains("2 authorized profile(s)"),
            "Render searchable profile access picker with selected and unavailable profiles"
        );
        await Write(
            output,
            "client-editor-access",
            "<div class=\"se-app\" style=\"padding:24px\"><div style=\"max-width:1200px;margin-inline:auto\">"
                + accessHtml
                + "</div></div>"
        );
        var releaseNotes = System.Text.Json.JsonSerializer.Deserialize<
            List<SkillsExtended.Models.ReleaseNote>
        >(
            await File.ReadAllTextAsync(
                Path.Combine(AppContext.BaseDirectory, "Resources", "ReleaseNotes.json")
            )
        )!;
        await using var releaseProvider = services.BuildServiceProvider();
        await using var releaseRenderer = new HtmlRenderer(
            releaseProvider,
            releaseProvider.GetRequiredService<ILoggerFactory>()
        );
        var releases = await releaseRenderer.Dispatcher.InvokeAsync(async () =>
        {
            RenderFragment body = builder =>
            {
                builder.OpenComponent<ReleaseNotesContent>(0);
                builder.AddAttribute(1, "Notes", releaseNotes);
                builder.CloseComponent();
            };
            var root = await releaseRenderer.RenderComponentAsync<BaseLayout>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Body"] = body })
            );
            return root.ToHtmlString();
        });
        check(
            releases.Contains("Release history")
                && releaseNotes.All(note => releases.Contains("Version " + note.Version)),
            "Render the themed release history with every existing version"
        );
        await Write(output, "release-notes", releases);
        foreach (var guide in typeof(Home).Assembly.GetTypes().Where(type =>
            type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template.StartsWith("/skills-extended/guides", StringComparison.Ordinal))))
        {
            var route = guide.GetCustomAttribute<RouteAttribute>()!.Template;
            var guideHtml = await homeRenderer.Dispatcher.InvokeAsync(async () =>
            {
                RenderFragment body = builder =>
                {
                    builder.OpenComponent(0, guide);
                    builder.CloseComponent();
                };
                var root = await homeRenderer.RenderComponentAsync<GuideLayout>(
                    ParameterView.FromDictionary(new Dictionary<string, object?> { ["Body"] = body }));
                return root.ToHtmlString();
            });
            check(guideHtml.Contains("loading=\"lazy\"") && guideHtml.Contains("se-guide-nav")
                && !guideHtml.Contains("se-savebar"), $"Render illustrated public guide {route}");
            if (route != "/skills-extended/guides")
                check(guideHtml.Contains("<figcaption>") && guideHtml.Contains("Controls")
                    && guideHtml.Contains("Success"), $"Guide {route} includes illustration legend, controls and outcomes");
            await Write(output, route == "/skills-extended/guides" ? "guides-index" : "guides-" + route.Split('/').Last(), guideHtml);
        }
        Console.WriteLine($"Rendered offline HTML fixtures: {output}");
    }

    private static async Task Write(string directory, string name, string markup)
    {
        // Only asset paths are rewritten; layout and page markup come from the real components.
        markup = markup.Replace("/skills-extended/icons/", "../icons/");
        markup = markup.Replace("src=\"/skills-extended/guides/", "src=\"../guides/");
        foreach (var image in new[] { "lockpicking", "hacking", "signals" })
            markup = markup.Replace($"href=\"/skills-extended/guides/{image}.png\"", $"href=\"../guides/{image}.png\"");
        var css = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "editor.css"));
        await File.WriteAllTextAsync(
            Path.Combine(directory, name + ".html"),
            "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><style>body{margin:0}"
                + css
                + "</style></head><body>"
                + markup
                + "</body></html>"
        );
    }
}

public class SilentLogger : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.ReturnType == typeof(bool) ? false : null;
}

sealed class OfflineJs : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args
    ) => ValueTask.FromResult(default(TValue)!);
}

sealed class OfflineNavigation : NavigationManager
{
    public OfflineNavigation() =>
        Initialize("http://offline.invalid/", "http://offline.invalid/skills-extended");

    protected override void NavigateToCore(string uri, bool forceLoad) { }
}
