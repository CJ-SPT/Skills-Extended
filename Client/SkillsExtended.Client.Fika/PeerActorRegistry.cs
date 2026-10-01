#nullable enable
using System;
using System.Collections.Generic;

namespace SkillsExtendedFika;

// Keys are connection objects, never reusable numeric peer IDs or display names.
internal sealed class PeerActorRegistry<TPeer>
    where TPeer : class
{
    private readonly Dictionary<TPeer, string> _actors = new();

    public void Bind(TPeer peer, string actor)
    {
        if (!string.IsNullOrEmpty(actor))
            _actors[peer] = actor;
    }

    public string? Actor(TPeer peer) => _actors.TryGetValue(peer, out var actor) ? actor : null;

    public bool Matches(TPeer peer, string actor) =>
        !string.IsNullOrEmpty(actor) && string.Equals(Actor(peer), actor, StringComparison.Ordinal);

    public void Remove(TPeer peer) => _actors.Remove(peer);

    public void Clear() => _actors.Clear();
}
