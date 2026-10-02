using System;
using System.IO;
using EFT;
using EFT.BinarySerialization;
using Mirror;

namespace SkillsExtended.Skills.Signals;

internal static class SignalsInventoryCodec
{
    private const string Prefix = "eft-item-v1:";

    // The envelope still carries a string. Use EFT's native polymorphic descriptor
    // codec inside it so nested resource, medical, durability and other components
    // retain their concrete types and current values without CLR type metadata.
    public static string Serialize(ItemDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        var writer = new NetworkWriter();
        writer.WriteEFTItemDescriptor(descriptor);
        return Prefix + Convert.ToBase64String(writer.ToArray());
    }

    public static ItemDescriptor Deserialize(string inventory)
    {
        if (inventory == null || !inventory.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidDataException("Signal inventory format mismatch. Install matching Skills Extended binaries on every client and host.");
        var reader = new NetworkReader(new ArraySegment<byte>(Convert.FromBase64String(inventory.Substring(Prefix.Length))));
        var descriptor = reader.ReadEFTItemDescriptor();
        if (reader.Remaining != 0)
            throw new InvalidDataException("Signal inventory contains trailing data.");
        return descriptor;
    }
}
