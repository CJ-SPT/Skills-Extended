using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillsExtended.Config.Skills;
using SkillsExtended.Core.Editing;
using SkillsExtended.Web.Shared;

internal static class AssistanceWebChecks
{
    internal static async Task Run(string source, Action<bool, string> check)
    {
        check(JsonSerializer.Deserialize<LockPickingData>("{}")!.EnableRaidCoaching
            && JsonSerializer.Deserialize<SignalsIntelligenceData>("{}")!.ShowCacheArrow,
            "Older server configurations preserve coaching and cache-arrow defaults");
        var temp = Path.Combine(Path.GetTempPath(), "SkillsExtended-Assistance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var name in new[] { "SkillsConfig.json", "ServerConfig.json" })
                File.Copy(Path.Combine(source, name), Path.Combine(temp, name));
            var store = new ConfigStore(temp, new ConfigFiles());
            var snapshot = await store.ReadSnapshotAsync();
            check(snapshot.Skills.LockPicking.EnableRaidCoaching && snapshot.Skills.SignalsIntelligence.ShowCacheArrow,
                "Shipped settings preserve existing assistance");
            var session = new EditorSession(snapshot);
            session.Skills.LockPicking.EnableRaidCoaching = false;
            session.Skills.SignalsIntelligence.ShowCacheArrow = false;
            check(session.PageChangesFor("LockPicking") == 1 && session.PageChangesFor("SignalsIntelligence") == 1
                && snapshot.Skills.LockPicking.EnableRaidCoaching && snapshot.Skills.SignalsIntelligence.ShowCacheArrow,
                "Assistance toggles track independent page changes without mutating saved settings");
            var result = await store.SaveAsync(session.Skills, session.Server, snapshot.Revision, _ => { });
            var saved = await new ConfigStore(temp, new ConfigFiles()).ReadSnapshotAsync();
            check(result.Success && !saved.Skills.LockPicking.EnableRaidCoaching && !saved.Skills.SignalsIntelligence.ShowCacheArrow,
                "Both disabled settings persist through a fresh server config store");

            using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
            await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                    { [nameof(AssistanceFields.Session)] = new EditorSession(saved) });
                return (await renderer.RenderComponentAsync<AssistanceFields>(parameters)).ToHtmlString();
            });
            check(html.Contains("LockPicking.EnableRaidCoaching") && html.Contains("Enable in-raid coaching")
                && html.Contains("SignalsIntelligence.ShowCacheArrow") && html.Contains("Show blue cache arrow")
                && !html.Contains("checked"), "Editor renders both saved toggles unchecked with readable labels");

            saved.Skills.LockPicking.EnableRaidCoaching = true;
            check((await store.SaveAsync(saved.Skills, saved.Server, saved.Revision, _ => { })).Success,
                "Raid coaching can be re-enabled independently");
            var independent = await store.ReadSnapshotAsync();
            check(independent.Skills.LockPicking.EnableRaidCoaching && !independent.Skills.SignalsIntelligence.ShowCacheArrow,
                "Re-enabling coaching leaves the cache arrow disabled");
        }
        finally { Directory.Delete(temp, true); }
    }
}

internal sealed class AssistanceFields : ComponentBase
{
    [Parameter] public EditorSession Session { get; set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<EditorSession>>(0);
        builder.AddAttribute(1, "Value", Session);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(content =>
        {
            foreach (var (skill, key) in new[] { ("LockPicking", "EnableRaidCoaching"), ("SignalsIntelligence", "ShowCacheArrow") })
            {
                content.OpenComponent<SettingField>(0);
                content.AddAttribute(1, "SkillKey", skill);
                content.AddAttribute(2, "Field", SkillCatalog.Fields[skill].Single(f => f.Key == key));
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}
