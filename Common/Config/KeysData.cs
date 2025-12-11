using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SkillsExtended.Config;

[DataContract]
public class KeysData
{
    [DataMember]
    public Dictionary<string, string> KeyLocale { get; set; }
    
    [DataMember]
    public Dictionary<string, string> ValueLocales { get; set; }
}