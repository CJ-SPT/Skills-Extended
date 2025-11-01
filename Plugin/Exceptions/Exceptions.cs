using System;

namespace SkillsExtended.Exceptions;

public class SkillsExtendedException(string message) : Exception(message);
public class LockPickingException(string message) : Exception(message);