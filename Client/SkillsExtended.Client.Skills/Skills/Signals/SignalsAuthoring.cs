using System;
using System.IO;
using System.Linq;
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
    [ConsoleCommand(
        "signals_capture",
        "",
        "Capture a candidate cache placement at the PMC position to the plugin folder"
    )]
    public static void Capture([ConsoleArgument("candidate")] string name)
    {
        var world = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
        if (!world?.MainPlayer || (world.LocationId != "bigmap" && world.LocationId != "woods"))
            return;
        var p = new SignalPlacement
        {
            Id = world.LocationId + "-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Map = world.LocationId,
            Name = name,
            Position = SignalsCase.Point(world.MainPlayer.Position),
            Yaw = world.MainPlayer.Rotation.x,
            Enabled = false,
        };
        var error = SignalsCase.ValidatePlacement(p, out var position);
        if (error != null)
        {
            ElectronicsRuntime.Notify(error);
            return;
        }
        p.Position = SignalsCase.Point(position);
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

    [ConsoleCommand(
        "signals_validate",
        "",
        "Validate configured cache placement surfaces on the loaded map"
    )]
    public static void Validate()
    {
        var world = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
        if (!world)
            return;
        foreach (
            var p in SkillsExtendedPlugin.SkillData.SignalsIntelligence.Placements.Where(p =>
                p.Map == world.LocationId
            )
        )
        {
            var error = SignalsCase.ValidatePlacement(p, out _);
            SkillsExtendedPlugin.Log.LogInfo(
                $"Signals placement {p.Id}: {error ?? "surface checks passed; inspect access and surroundings"}"
            );
        }
        ElectronicsRuntime.Notify("Signal placement results written to the game log.");
    }

    [ConsoleCommand(
        "signals_preview",
        "",
        "Show an unlootable placement marker for 20 seconds; no rewards or XP"
    )]
    public static void Preview([ConsoleArgument("")] string id)
    {
        var world = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
        var p = SkillsExtendedPlugin.SkillData.SignalsIntelligence.Placements.FirstOrDefault(p =>
            p.Id == id && p.Map == world?.LocationId
        );
        if (p == null)
        {
            ElectronicsRuntime.Notify("Unknown placement ID on this map.");
            return;
        }
        var error = SignalsCase.ValidatePlacement(p, out var position);
        if (error != null)
        {
            ElectronicsRuntime.Notify(error);
            return;
        }
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Signal placement preview";
        marker.transform.position = position + Vector3.up * .2f;
        marker.transform.localScale = new Vector3(.5f, .4f, .3f);
        UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
        UnityEngine.Object.Destroy(marker, 20);
    }
}
