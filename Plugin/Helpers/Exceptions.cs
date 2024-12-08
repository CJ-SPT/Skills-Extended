using System;

namespace SkillsExtended.Helpers;

public class SkillsExtendedException(string message) : Exception(message)
{
    
}

public class LockPickingException(string message) : Exception(message)
{
    
}