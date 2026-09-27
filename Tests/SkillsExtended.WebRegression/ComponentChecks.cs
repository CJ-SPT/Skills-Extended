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
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
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
