using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillsExtended.Core.Editing;
using SkillsExtended.Web.Shared;

// This test renderer dispatches actual Blazor handlers without a browser or SPT host.
#pragma warning disable BL0006
public static class ComponentChecks
{
    public static async Task Run(ConfigSnapshot snapshot, Action<bool, string> check)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(RewardCatalogChecks.Templates())
            .AddSingleton(RewardCatalogChecks.Locales())
            .BuildServiceProvider();
        await using var renderer = new EventRenderer(
            services,
            services.GetRequiredService<ILoggerFactory>()
        );
        var session = new EditorSession(snapshot);
        var changes = 0;
        session.Changed += () => changes++;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.Mount(new FieldHost(session));
            var field = SkillCatalog.Fields["FirstAid"].Single(f => f.Key == "XpPerAction");
            var before = session.Skills.FirstAid.XpPerAction;
            await renderer.Input(root, "FirstAid.XpPerAction", "oninput", "invalid");
            check(
                session.InputErrors.ContainsKey("FirstAid.XpPerAction")
                    && session.Skills.FirstAid.XpPerAction == before,
                "Real numeric handler retains invalid text without changing config"
            );
            await renderer.Input(root, "FirstAid.XpPerAction", "oninput", "0.0025");
            check(
                session.InputErrors.Count == 0
                    && session.Skills.FirstAid.XpPerAction == .0025f
                    && changes == 2,
                "Real numeric handler clears errors and notifies the editor"
            );
            await renderer.Input(root, "FirstAid.Enabled", "onchange", false);
            check(
                !session.Skills.FirstAid.Enabled && snapshot.Skills.FirstAid.Enabled,
                "Real switch edits only the draft"
            );
            session.Reset(snapshot);
            await renderer.Refresh(root);
            check(
                !session.Dirty && session.Skills.FirstAid.XpPerAction == before,
                "Rendered fields follow discarded session data"
            );

            var pins = await renderer.Mount(new TablesHost(session, typeof(LockPinTiersEditor)));
            foreach (var invalid in new[] { "", "NaN", "2", "6", "3.5" })
            {
                await renderer.Input(pins, "LockPicking.Tiers.1.Pins", "oninput", invalid);
                check(
                    session.InputErrors.ContainsKey("LockPicking.Tiers.1.Pins")
                        && session.Skills.LockPicking.Tiers[0].Pins == 3,
                    "Invalid pin counts remain visible without modifying the tier"
                );
            }
            await renderer.Input(pins, "LockPicking.Tiers.1.Pins", "oninput", "4");
            await renderer.Input(pins, "LockPicking.Tiers.1.Tolerance", "oninput", "0.075");
            await renderer.Input(
                pins,
                "LockPicking.Tiers.1.StrainWarningSeconds",
                "oninput",
                "0.8"
            );
            check(
                session.InputErrors.Count == 0
                    && session.Skills.LockPicking.Tiers[0].Pins == 4
                    && session.Skills.LockPicking.Tiers[0].Tolerance == .075f
                    && session.Skills.LockPicking.Tiers[0].StrainWarningSeconds == .8f
                    && snapshot.Skills.LockPicking.Tiers[0].Pins == 3
                    && session.Dirty,
                "Real pin handlers edit an isolated draft and retain fractional settings"
            );
            await renderer.Input(pins, "LockPicking.Tiers.1.Tolerance", "oninput", "0.01");
            check(
                session.InputErrors.ContainsKey("LockPicking.Tiers.1.Tolerance"),
                "Pin tolerance lower bound blocks invalid saves"
            );
            session.Reset(snapshot);
            await renderer.Refresh(pins);
            check(
                !session.Dirty && session.Skills.LockPicking.Tiers[0].Pins == 3,
                "Discard restores pin settings and clears invalid input"
            );

            var rewards = await renderer.Mount(
                new TablesHost(session, typeof(SignalsTablesEditor))
            );
            var originalCount = session.Skills.SignalsIntelligence.Loot.Count;
            await renderer.Input(rewards, "reward-search", "oninput", "precision circuit");
            check(!session.Dirty, "Searching server items does not change the draft");
            await renderer.Input(rewards, "reward-new-theme", "oninput", "Custom treasures");
            await renderer.Input(rewards, "reward-new-weight", "onchange", "3");
            await renderer.Click(rewards, "reward-add-" + RewardCatalogChecks.ModItem);
            var added = session.Skills.SignalsIntelligence.Loot.Last();
            check(
                added.Template == RewardCatalogChecks.ModItem
                    && added.Theme == "Custom treasures"
                    && added.Weight == 3
                    && session.Dirty
                    && snapshot.Skills.SignalsIntelligence.Loot.Count == originalCount,
                "Real picker fills the template ID, theme, and weight in the isolated draft"
            );
            await renderer.Click(rewards, "reward-add-" + RewardCatalogChecks.ModItem);
            check(
                session.Skills.SignalsIntelligence.Loot.Count == originalCount + 1,
                "Duplicate clicks cannot add the same item to a theme twice"
            );
            await renderer.Input(rewards, "reward-new-theme", "oninput", "Second theme");
            await renderer.Click(rewards, "reward-add-" + RewardCatalogChecks.ModItem);
            check(
                session.Skills.SignalsIntelligence.Loot.Count == originalCount + 2,
                "The same item can be added to a different theme"
            );
            await renderer.Input(rewards, "reward-search", "oninput", "quest circuit");
            await renderer.Input(rewards, "reward-eligible", "onchange", false);
            await renderer.Click(rewards, "reward-add-" + RewardCatalogChecks.QuestItem);
            check(
                session.Skills.SignalsIntelligence.Loot.Count == originalCount + 2,
                "Ineligible search results cannot be added even when their handler is invoked"
            );
            session.Reset(snapshot);
            await renderer.Refresh(rewards);
            check(
                !session.Dirty && session.Skills.SignalsIntelligence.Loot.Count == originalCount,
                "Discard restores configured rewards after picker additions"
            );

            var originalLocations = session.Skills.SignalsIntelligence.Placements.Count;
            var woodsIndex = session.Skills.SignalsIntelligence.Placements.FindIndex(p =>
                p.Map == "woods"
            );
            await renderer.Input(rewards, "location-search", "oninput", "Customs");
            check(
                !session.Dirty
                    && renderer.HasHandler(rewards, "location-toggle-0", "onclick")
                    && !renderer.HasHandler(rewards, $"location-toggle-{woodsIndex}", "onclick"),
                "Friendly map-name search filters locations without editing the draft"
            );
            await renderer.Input(rewards, "location-search", "oninput", "");
            await renderer.Input(rewards, "location-status", "onchange", "disabled");
            check(
                !renderer.HasHandler(rewards, "location-toggle-0", "onclick") && !session.Dirty,
                "Disabled-only filter hides enabled locations without changing their status"
            );
            await renderer.Click(rewards, "location-add-woods");
            var location = session.Skills.SignalsIntelligence.Placements[woodsIndex];
            var locationId = location.Id;
            check(
                location.Map == "woods"
                    && !location.Enabled
                    && location.SearchRadius == 10
                    && renderer.HasHandler(rewards, $"location-toggle-{woodsIndex}", "onclick")
                    && session
                        .Skills.SignalsIntelligence.Placements.Select(p => p.Id)
                        .Distinct()
                        .Count()
                        == originalLocations + 1
                    && snapshot.Skills.SignalsIntelligence.Placements.Count == originalLocations,
                "Map-specific add creates a visible disabled draft location with a unique ID"
            );
            await renderer.Input(rewards, $"location-name-{woodsIndex}", "onchange", "Test ridge");
            await renderer.Input(rewards, $"location-x-{woodsIndex}", "onchange", "123.456");
            await renderer.Input(rewards, $"location-enabled-{woodsIndex}", "onchange", true);
            await renderer.Input(
                rewards,
                $"location-map-select-{woodsIndex}",
                "onchange",
                "bigmap"
            );
            check(
                location.Map == "bigmap"
                    && location.Name == "Test ridge"
                    && location.Id == locationId
                    && location.Position.X == 123.456f
                    && location.Enabled,
                "Moving a location between map groups preserves its ID and precise edited coordinates"
            );
            await renderer.Click(rewards, "locations-expand");
            await renderer.Click(rewards, "locations-collapse");
            await renderer.Click(rewards, $"location-remove-{woodsIndex}");
            check(
                session.Skills.SignalsIntelligence.Placements.Count == originalLocations
                    && !session.Dirty,
                "Removing the added location restores the original draft content"
            );
            await renderer.Click(rewards, "location-add-bigmap");
            session.Reset(snapshot);
            await renderer.Refresh(rewards);
            check(
                !session.Dirty
                    && session.Skills.SignalsIntelligence.Placements.Count == originalLocations,
                "Discard resets grouped locations and removes newly added entries"
            );
            check(renderer.HasHandler(rewards, "location-add-laboratory", "onclick")
                && renderer.HasHandler(rewards, "location-add-labyrinth", "onclick")
                && !renderer.HasHandler(rewards, "location-add-factory4_day", "onclick")
                && !session.Dirty,
                "Expanded map controls appear without generating draft locations or admitting Factory");
            await renderer.Click(rewards, "location-add-laboratory");
            check(session.Skills.SignalsIntelligence.Placements.Count == originalLocations + 1
                && session.Skills.SignalsIntelligence.Placements.Single(p => p.Map == "laboratory").Enabled == false
                && ConfigRules.Validate(session.Skills).Count == 0,
                "Explicitly adding a new-map web location creates one valid disabled draft");
            session.Reset(snapshot);
        });
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var boards = await renderer.Mount(new TablesHost(session, typeof(HackingBoardsEditor)));
            var standard = session.Skills.Hacking.Tier(1);
            var originalNodes = standard.Nodes;
            await renderer.Input(boards, "Hacking.Tiers.1.Nodes", "oninput", "");
            check(
                standard.Nodes == originalNodes
                    && session.InputErrors.ContainsKey("Hacking.Tiers.1.Nodes"),
                "Empty board input blocks saving without replacing the last valid node count"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.Nodes", "oninput", "62");
            check(
                standard.Nodes == originalNodes
                    && session.InputErrors.ContainsKey("Hacking.Tiers.1.Nodes"),
                "Board node limits reject oversized boards"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.Nodes", "oninput", "15");
            await renderer.Input(boards, "Hacking.Tiers.1.Defenses", "oninput", "7");
            check(
                session.InputErrors.ContainsKey("Hacking.Tiers.1.Layout")
                    && ConfigRules.Validate(session.Skills).Count > 0,
                "Board composition detects insufficient reserved nodes and matches backend validation"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.Defenses", "oninput", "4");
            check(
                session.InputErrors.Count == 0 && ConfigRules.Validate(session.Skills).Count == 0,
                "Exactly eight reserved nodes is accepted without changing the gameplay rule"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.Defenses", "oninput", "3.5");
            check(
                standard.Defenses == 4
                    && session.InputErrors.ContainsKey("Hacking.Tiers.1.Defenses"),
                "Board content counts reject fractional numbers"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.Defenses", "oninput", "4");
            await renderer.Input(boards, "Hacking.Tiers.1.SuccessXp", "oninput", "0.125");
            check(
                standard.SuccessXp == .125f && snapshot.Skills.Hacking.Tier(1).SuccessXp != .125f,
                "Board reward input preserves fractional XP in the isolated draft"
            );
            await renderer.Input(boards, "Hacking.Tiers.1.CoreCoherence", "oninput", "0");
            check(
                standard.CoreCoherence > 0
                    && session.InputErrors.ContainsKey("Hacking.Tiers.1.CoreCoherence"),
                "Core health cannot be set below its supported minimum"
            );
            session.Reset(snapshot);
            await renderer.Refresh(boards);
            check(
                !session.Dirty
                    && session.InputErrors.Count == 0
                    && session.Inputs.Count == 0
                    && session.Skills.Hacking.Tier(1).Nodes == originalNodes,
                "Discard restores all difficulty settings and clears board input errors"
            );
        });
        services.Dispose();
    }

    private sealed class FieldHost(EditorSession session) : ComponentBase
    {
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder
        )
        {
            builder.OpenComponent<CascadingValue<EditorSession>>(0);
            builder.AddAttribute(1, "Value", session);
            builder.AddAttribute(
                2,
                "ChildContent",
                (RenderFragment)(
                    content =>
                    {
                        foreach (var key in new[] { "XpPerAction", "Enabled" })
                        {
                            content.OpenComponent<SettingField>(0);
                            content.AddAttribute(1, "SkillKey", "FirstAid");
                            content.AddAttribute(
                                2,
                                "Field",
                                SkillCatalog.Fields["FirstAid"].Single(f => f.Key == key)
                            );
                            content.CloseComponent();
                        }
                    }
                )
            );
            builder.CloseComponent();
        }
    }

    private sealed class TablesHost(EditorSession session, Type editorType) : ComponentBase
    {
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder
        )
        {
            builder.OpenComponent<CascadingValue<EditorSession>>(0);
            builder.AddAttribute(1, "Value", session);
            builder.AddAttribute(
                2,
                "ChildContent",
                (RenderFragment)(
                    content =>
                    {
                        content.OpenComponent(0, editorType);
                        content.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();
        }
    }

    private sealed class EventRenderer(IServiceProvider services, ILoggerFactory logger)
        : Renderer(services, logger)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) =>
            Task.CompletedTask;

        protected override void HandleException(Exception exception) => throw exception;

        public async Task<int> Mount(IComponent component)
        {
            var id = AssignRootComponentId(component);
            await RenderRootComponentAsync(id);
            return id;
        }

        public Task Refresh(int root) => RenderRootComponentAsync(root);

        public bool HasHandler(int root, string elementId, string eventName) =>
            FindHandler(root, elementId, eventName) != 0;

        public Task Click(int root, string elementId)
        {
            var handler = FindHandler(root, elementId, "onclick");
            if (handler == 0)
            {
                throw new InvalidOperationException($"Missing click handler {elementId}");
            }
            return DispatchEventAsync(
                handler,
                null,
                new Microsoft.AspNetCore.Components.Web.MouseEventArgs()
            );
        }

        public Task Input(int root, string elementId, string eventName, object value)
        {
            var handler = FindHandler(root, elementId, eventName);
            if (handler == 0)
                throw new InvalidOperationException($"Missing handler {elementId} / {eventName}");
            return DispatchEventAsync(handler, null, new ChangeEventArgs { Value = value });
        }

        private ulong FindHandler(int componentId, string elementId, string eventName)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames.Array[i];
                if (frame.FrameType == RenderTreeFrameType.Component)
                {
                    var nested = FindHandler(frame.ComponentId, elementId, eventName);
                    if (nested != 0)
                        return nested;
                }
                if (frame.FrameType != RenderTreeFrameType.Element)
                    continue;
                var matches = false;
                ulong handler = 0;
                for (
                    var j = i + 1;
                    j < frames.Count && frames.Array[j].FrameType == RenderTreeFrameType.Attribute;
                    j++
                )
                {
                    var attribute = frames.Array[j];
                    if (
                        attribute.AttributeName == "id"
                        && Equals(attribute.AttributeValue, elementId)
                    )
                        matches = true;
                    if (attribute.AttributeName == eventName)
                        handler = attribute.AttributeEventHandlerId;
                }
                if (matches)
                    return handler;
            }
            return 0;
        }
    }
}
#pragma warning restore BL0006
