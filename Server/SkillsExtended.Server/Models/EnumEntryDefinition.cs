using System.Runtime.Serialization;

namespace SkillsExtended.Models;

[DataContract]
public sealed record EnumEntryDefinition
{
    [DataMember]
    public required string EnumType { get; init; }

    [DataMember]
    public required string Name { get; init; }

    [DataMember]
    public string? AttributeName { get; init; }

    [DataMember]
    public required int Value { get; init; }
}
