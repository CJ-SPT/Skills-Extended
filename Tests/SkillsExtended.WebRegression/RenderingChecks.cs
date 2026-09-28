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
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;

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
        services.AddSingleton(
            new LocaleService(
                DispatchProxy.Create<ISptLogger<LocaleService>, SilentLogger>(),
                new LocaleTable
                {
                    Global = [],
                    Menu = [],
                    Languages = [],
                },
                new LocaleConfig
                {
                    GameLocale = "en",
                    ServerLocale = "en",
                    ServerSupportedLocales = ["en"],
                    Fallbacks = [],
                }
            )
        );
        var output = Path.Combine(AppContext.BaseDirectory, "rendered");
        Directory.CreateDirectory(output);
        foreach (var skill in SkillCatalog.All)
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
                    builder.OpenComponent<SkillEditor>(0);
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
            home.Contains($"{SkillCatalog.All.Count} skills") && home.Contains("se-card-grid"),
            "Render actual overview with all skill cards"
        );
        await Write(output, "overview", home);
        Console.WriteLine($"Rendered offline HTML fixtures: {output}");
    }

    private static async Task Write(string directory, string name, string markup)
    {
        // Only asset paths are rewritten; layout and page markup come from the real components.
        markup = markup.Replace("/skills-extended/icons/", "../icons/");
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
