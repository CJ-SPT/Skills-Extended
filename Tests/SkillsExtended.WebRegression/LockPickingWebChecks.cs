using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillsExtended.Core.Editing;
using SkillsExtended.Web.Shared;

internal static class LockPickingWebChecks
{
    internal static async Task Run(string source, Action<bool, string> check, bool preview)
    {
        var temp = Path.Combine(Path.GetTempPath(), "SkillsExtended-LockWeb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var name in new[] { "SkillsConfig.json", "ServerConfig.json" })
                File.Copy(Path.Combine(source, name), Path.Combine(temp, name));
            var store = new ConfigStore(temp, new ConfigFiles());
            var draft = await store.ReadSnapshotAsync();
            var tier = draft.Skills.LockPicking.Tier(5);
            tier.SpoolPins = 1; tier.SerratedPins = 3; tier.SerrationCatches = 2;
            var result = await store.SaveAsync(draft.Skills, draft.Server, draft.Revision, _ => { });
            var saved = await store.ReadSnapshotAsync();
            check(result.Success && saved.Skills.LockPicking.Tier(5).SpoolPins == 1
                && saved.Skills.LockPicking.Tier(5).SerratedPins == 3
                && saved.Skills.LockPicking.Tier(5).SerrationCatches == 2,
                "Security pin composition survives actual web-store save and reload");
            saved.Skills.LockPicking.Tier(5).SerratedPins = 5;
            check((await store.SaveAsync(saved.Skills, saved.Server, saved.Revision, _ => { })).Status == EditStatus.Validation,
                "Invalid security composition cannot be saved");
            check((await store.ReadSnapshotAsync()).Skills.LockPicking.Tier(5).SerratedPins == 3,
                "Rejected security draft preserves saved configuration");
            var current = await store.ReadSnapshotAsync();
            current.Skills.LockPicking.Tier(3).SerrationCatches = 0;
            check(ConfigRules.Validate(current.Skills).Any(), "Serrated pins require at least one catch");
            var services = new ServiceCollection();
            services.AddLogging();
            using var provider = services.BuildServiceProvider();
            await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                { [nameof(LockPickingEditorHost.Session)] = new EditorSession(await store.ReadSnapshotAsync()) });
                var component = await renderer.RenderComponentAsync<LockPickingEditorHost>(parameters);
                return component.ToHtmlString();
            });
            check(html.Contains("Spool pins") && html.Contains("Serrated pins")
                && html.Contains("LockPicking.Tiers.5.SerrationCatches"),
                "Rendered tier editor includes accessible security controls");
            if (preview)
            {
                var output = Path.GetFullPath("artifacts/lockpicking/web-preview.html");
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                var css = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "editor.css"));
                await File.WriteAllTextAsync(output, "<!doctype html><meta name=viewport content=\"width=device-width,initial-scale=1\"><style>" + css
                    + "</style><main class=\"se-app\" style=\"padding:20px;max-width:1200px;margin:auto\">" + html + "</main>");
                Console.WriteLine("Rendered preview: " + output);
            }
        }
        finally { Directory.Delete(temp, true); }
    }
}

internal sealed class LockPickingEditorHost : ComponentBase
{
    [Parameter] public EditorSession Session { get; set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<EditorSession>>(0);
        builder.AddAttribute(1, "Value", Session);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(child =>
        {
            child.OpenComponent<LockPinTiersEditor>(0);
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
