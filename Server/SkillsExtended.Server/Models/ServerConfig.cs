using System.Runtime.Serialization;

namespace SkillsExtended.Models;

[DataContract]
public record ServerConfig
{
    [DataMember]
    public required bool CheckForUpdates { get; set; }
}