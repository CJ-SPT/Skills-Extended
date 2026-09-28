using EFT.Interactive;
using SkillsExtended.Config.Skills;
using SkillsExtended.Hacking;
using SkillsExtended.Skills.Hacking;

public static class ElectronicsDoorChecks
{
    public static void Run(Action<bool, string> check)
    {
        var factory = new KeycardDoor
        {
            Id = "door_Factory_Rework_Basement_00031",
            KeyId = "66acd6702b17692df20144c0",
        };
        var custom = new DerivedKeypad { Id = "custom-keypad" };
        var mechanical = new WorldInteractiveObject { Id = "mechanical" };
        LocationScene.Objects = [factory, custom, mechanical, null];
        check(!LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<KeycardDoor>().Any(),
            "Native scene registry does not expose a separate KeycardDoor array");
        var registry = new ElectronicsDoorRegistry();
        registry.Refresh();
        check(ReferenceEquals(registry.Resolve(factory.Id), factory),
            "Factory keypad resolves from the native WorldInteractiveObject registry");
        check(ReferenceEquals(registry.Resolve(custom.Id), custom) && ElectronicsDoorRegistry.Supports(custom),
            "Derived keypad with no keycard ID remains hackable");
        check(registry.Resolve(mechanical.Id) is null, "Mechanical locks are not electronic readers");

        var config = new HackingData();
        var authority = new HackingAuthority(config, 42);
        var resolved = registry.Resolve(factory.Id);
        var reply = authority.Process(new HackRequest
        {
            Raid = authority.Raid, Actor = "pmc", Door = factory.Id, Operation = "start",
        }, 0, config.Difficulty("factory4_day", resolved.Id, resolved.KeyId),
            ElectronicsDoorRegistry.Supports(resolved) ? null : "Electronic lock is unavailable");
        check(reply.Error is null && reply.Board?.Status == HackStatus.Active,
            "Displayed Factory keypad starts an authoritative hacking attempt");

        var late = new KeycardDoor { Id = "late-reader" };
        LocationScene.Objects = [factory, custom, late];
        check(ReferenceEquals(registry.Resolve(late.Id), late), "Host resolves readers loaded after startup");
        var peerRegistry = new ElectronicsDoorRegistry();
        check(ReferenceEquals(peerRegistry.Resolve(factory.Id), factory), "Peer resolves the same reader for an unlock reply");
        factory.Destroyed = true;
        var replacement = new KeycardDoor { Id = factory.Id };
        LocationScene.Objects = [replacement];
        check(ReferenceEquals(registry.Resolve(factory.Id), replacement), "Destroyed cached reader is replaced from the current scene");
        var dynamicReader = new KeycardDoor { Id = "observed-reader" };
        registry.Register(dynamicReader);
        check(ReferenceEquals(registry.Resolve(dynamicReader.Id), dynamicReader), "Interaction target can register a dynamic keypad directly");
        dynamicReader.Destroyed = true;
        check(registry.Resolve(dynamicReader.Id) is null, "Destroyed reader cannot be used");
        check(registry.Resolve(null) is null && registry.Resolve("") is null && registry.Resolve("missing") is null,
            "Missing reader IDs fail cleanly");
        check(!ElectronicsDoorRegistry.Supports(null) && !ElectronicsDoorRegistry.Supports(new KeycardDoor()),
            "Only live readers with a stable door ID are supported");
        LocationScene.Objects = [];
    }

    private sealed class DerivedKeypad : KeycardDoor;
}

// Model the game's exact-type scene container, rather than an assignable-type search.
public static class LocationScene
{
    public static WorldInteractiveObject[] Objects = [];
    public static IEnumerable<T> GetAllObjectsAndWhenISayAllIActuallyMeanIt<T>() =>
        typeof(T) == typeof(WorldInteractiveObject) ? Objects.Cast<T>() : [];
}

namespace EFT.Interactive
{
    public class KeycardDoor : WorldInteractiveObject;
}
