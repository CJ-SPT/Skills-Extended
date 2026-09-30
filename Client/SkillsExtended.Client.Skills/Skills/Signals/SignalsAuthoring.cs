using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using Newtonsoft.Json;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

public class SignalsAuthoring
{
    private static bool _busy;

    [ConsoleCommand(
        "signals_capture",
        "",
        "Capture a disabled exact cache placement at the PMC position"
    )]
    public static void Capture([ConsoleArgument("candidate")] string name) =>
        Run(
            async (world, runtime) =>
            {
                if (!world.MainPlayer)
                    return;
                var p = new SignalPlacement
                {
                    Id =
                        SignalsMaps.Normalize(world.LocationId)
                        + "-"
                        + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Map = SignalsMaps.Normalize(world.LocationId),
                    Name = name,
                    Position = SignalsCase.Point(world.MainPlayer.Position),
                    Yaw = world.MainPlayer.Rotation.x,
                    SearchRadius = 0,
                    Enabled = false,
                };
                var report = await SignalsPlacement.Resolve(
                    runtime,
                    new[] { p },
                    0,
                    runtime.Lifetime
                );
                if (report.Placement == null)
                {
                    ElectronicsRuntime.Notify("Capture rejected. " + report);
                    return;
                }
                // Store the ground anchor, not the prefab root offset; resolving a captured
                // point again must not repeatedly add the case's pivot/ground clearance.
                p.Yaw = report.Placement.Yaw;
                p.Position = report.Placement.Position;
                var body = SignalsPlacement.Geometry().Body;
                p.Position.Y += body.Center.Y - body.Extents.Y - .02f;
                var directory = Path.Combine(
                    Path.GetDirectoryName(typeof(SignalsAuthoring).Assembly.Location),
                    "SignalsPlacements"
                );
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, p.Id + ".json"),
                    JsonConvert.SerializeObject(p, Formatting.Indented)
                );
                ElectronicsRuntime.Notify(
                    "Captured " + p.Id + ". Review and add it in the Signals editor."
                );
            }
        );

    [ConsoleCommand(
        "signals_validate",
        "",
        "Resolve configured cache areas on the loaded map and log rejection counts"
    )]
    public static void Validate() =>
        Run(
            async (world, runtime) =>
            {
                foreach (
                    var p in SkillsExtendedPlugin.SkillData.SignalsIntelligence.Placements.Where(
                        p => SignalsMaps.Same(p.Map, world.LocationId)
                    )
                )
                {
                    var report = await SignalsPlacement.Resolve(
                        runtime,
                        new[] { SignalPlacementSearch.Copy(p) },
                        0,
                        runtime.Lifetime
                    );
                    SkillsExtendedPlugin.Log.LogInfo(
                        $"Signals area {p.Id} (enabled={p.Enabled}): {report}"
                    );
                }
                ElectronicsRuntime.Notify(
                    "Signal placement results written to the game log; inspect access in-game."
                );
            }
        );

    [ConsoleCommand(
        "signals_preview",
        "",
        "Show the resolved unlootable case for 20 seconds; no rewards or XP"
    )]
    public static void Preview([ConsoleArgument("")] string id) =>
        Run(
            async (world, runtime) =>
            {
                var p =
                    SkillsExtendedPlugin.SkillData.SignalsIntelligence.Placements.FirstOrDefault(
                        p => p.Id == id && SignalsMaps.Same(p.Map, world.LocationId)
                    );
                if (p == null)
                {
                    ElectronicsRuntime.Notify("Unknown placement ID on this map.");
                    return;
                }
                var report = await SignalsPlacement.Resolve(
                    runtime,
                    new[] { SignalPlacementSearch.Copy(p) },
                    0,
                    runtime.Lifetime
                );
                if (report.Placement == null)
                {
                    ElectronicsRuntime.Notify("Preview rejected. " + report);
                    return;
                }
                var marker = new GameObject("Signal placement preview");
                marker.SetActive(false);
                try
                {
                    marker.transform.SetPositionAndRotation(
                        SignalsCase.Vector(report.Placement.Position),
                        Quaternion.Euler(0, report.Placement.Yaw, 0)
                    );
                    var clone = SignalsCase.InstantiateVisual(marker.transform);
                    foreach (var behavior in clone.GetComponentsInChildren<MonoBehaviour>(true))
                        behavior.enabled = false;
                    foreach (var collider in clone.GetComponentsInChildren<Collider>(true))
                        collider.enabled = false;
                    marker.AddComponent<SignalsPlacementPreview>();
                    clone.SetActive(true);
                    marker.SetActive(true);
                    UnityEngine.Object.Destroy(marker, 20);
                }
                catch
                {
                    UnityEngine.Object.Destroy(marker);
                    throw;
                }
            }
        );

    private static async void Run(Func<GameWorld, SignalsRuntime, Task> operation)
    {
        var world = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
        var runtime = SignalsRuntime.Instance;
        if (
            !world
            || !runtime
            || runtime.World != world
            || !SignalsMaps.IsSupported(world.LocationId)
        )
        {
            ElectronicsRuntime.Notify("Load Customs or Woods to author signal placements.");
            return;
        }
        if (_busy)
        {
            ElectronicsRuntime.Notify("A signal placement check is already running.");
            return;
        }
        _busy = true;
        try
        {
            await operation(world, runtime);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            SkillsExtendedPlugin.Log.LogError("Signals authoring: " + e);
            ElectronicsRuntime.Notify("Signal placement check failed: " + e.Message);
        }
        finally
        {
            _busy = false;
        }
    }
}
