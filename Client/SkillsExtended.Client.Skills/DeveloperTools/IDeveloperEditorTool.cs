using System;
using System.Threading;
using System.Threading.Tasks;
using EFT;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillsExtended.DeveloperTools;

internal interface IDeveloperEditorTool : IDisposable
{
    string Id { get; }
    string Title { get; }
    bool Supports(string map);
    bool Busy { get; }
    bool Dirty { get; }
    Task Activate(DeveloperEditorContext context);
    void Deactivate();
    void BuildActions(VisualElement parent);
    void RefreshList();
    void Tick();
    void WorldInput(Ray ray);
    bool Cancel();
    void Undo(bool redo);
    void Frame();
    Task Save();
    Task Reload();
}

internal sealed class DeveloperEditorContext
{
    internal MonoBehaviour Runner;
    internal GameWorld World;
    internal string Raid;
    internal DeveloperEditorView View;
    internal Func<Camera> Camera;
    internal Func<bool> IsOpen;
    internal Func<CancellationToken> Cancellation;
    internal Action<Vector3> Frame;
    internal void Status(string message) => View.Status(message);
}
