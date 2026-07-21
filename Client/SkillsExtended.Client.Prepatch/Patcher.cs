using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Newtonsoft.Json;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using Logger = BepInEx.Logging.Logger;

namespace SkillsExtended;

public static class SkillsExtendedPatcher
{
    private const string EnumEntriesRoute = "/skills-extended/early-init";

    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];
    private static TypeDefinition? _skillManager;

    private static readonly string PatcherPath =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new FileNotFoundException("Could not find patcher path");

    private static readonly string PluginPath = Path.Combine(
        PatcherPath,
        "..",
        "plugins",
        "SkillsExtended",
        "SkillsExtended.Client.API.dll"
    );

    public static void Patch(ref AssemblyDefinition assembly)
    {
#if !DEBUG
        if (!File.Exists(PluginPath))
        {
            var result = MessageBoxHelper.Show(
                @"Could not find BepInEx\plugins\SkillsExtended\SkillsExtended.dll in the plugins folder. Mod is not installed correctly. Exiting.",
                "Skills Extended error.",
                MessageBoxHelper.MessageBoxType.OK
            );

            Environment.Exit(1);
            return;
        }
#endif
        try
        {
            _skillManager = assembly.MainModule.GetType("EFT.SkillManager");

            var enumEntries = GetEnumEntries();
            PatchEnums(ref assembly, enumEntries);
            PatchSkillManager(ref assembly);

            Logger
                .CreateLogSource("Skills Extended PrePatch")
                .LogInfo($"Patching Complete! Added {enumEntries.Count} enum entries.");
        }
        catch (Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            Logger
                .CreateLogSource("Skills Extended PrePatch")
                .LogError("Error When Patching: " + ex.Message + " - Line " + line);
        }
    }

    private static FieldDefinition CreateNewEnum(
        ref AssemblyDefinition assembly,
        string? attributeName,
        string enumName,
        TypeDefinition enumClass,
        int customConstant
    )
    {
        var enumAttributeClass = assembly.MainModule.GetType("EFT.JsonEnumNameAttribute");

        var attributeConstructor = enumAttributeClass.Methods.First(m => m.IsConstructor);

        var attribute = new CustomAttribute(attributeConstructor);

        if (attributeName is not null)
        {
            var valueArgument = new CustomAttributeArgument(
                assembly.MainModule.TypeSystem.String,
                attributeName
            );
            attribute.ConstructorArguments.Add(valueArgument);
        }

        var newEnum = new FieldDefinition(
            enumName,
            FieldAttributes.Public
                | FieldAttributes.Static
                | FieldAttributes.Literal
                | FieldAttributes.HasDefault,
            enumClass
        )
        {
            Constant = customConstant,
        };

        newEnum.CustomAttributes.Add(attribute);

        return newEnum;
    }

    private static List<EnumEntryDefinition> GetEnumEntries()
    {
        var backendUrl = GetBackendUrl();
        var requestUri = new Uri(
            new Uri(backendUrl.TrimEnd('/') + "/"),
            EnumEntriesRoute.TrimStart('/')
        );

        var response = WinHttpClient.GetString(requestUri);
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException(
                $"The server returned an empty response from {EnumEntriesRoute}."
            );
        }

        var entries = JsonConvert.DeserializeObject<List<EnumEntryDefinition>>(response);
        if (entries is null || entries.Count == 0)
        {
            throw new InvalidOperationException(
                $"The server returned no enum entries from {EnumEntriesRoute}."
            );
        }

        return entries;
    }

    private static string GetBackendUrl()
    {
        const string configPrefix = "-config=";
        var configArgument = Environment
            .GetCommandLineArgs()
            .FirstOrDefault(argument =>
                argument.StartsWith(configPrefix, StringComparison.OrdinalIgnoreCase)
            );

        if (configArgument is null)
        {
            throw new InvalidOperationException(
                "Could not find SPT's -config launch argument containing the backend URL."
            );
        }

        var launcherConfig = JsonConvert.DeserializeObject<LauncherConfig>(
            configArgument.Substring(configPrefix.Length)
        );
        if (string.IsNullOrWhiteSpace(launcherConfig?.BackendUrl))
        {
            throw new InvalidOperationException(
                "SPT's -config launch argument did not contain a backend URL."
            );
        }

        return launcherConfig.BackendUrl;
    }

    private static void PatchEnums(
        ref AssemblyDefinition assembly,
        IReadOnlyCollection<EnumEntryDefinition> entries
    )
    {
        foreach (var enumGroup in entries.GroupBy(entry => entry.EnumType))
        {
            var enumType = assembly.MainModule.GetType(enumGroup.Key);
            if (enumType is null || !enumType.IsEnum)
            {
                throw new InvalidOperationException(
                    $"Could not find enum type '{enumGroup.Key}' in Assembly-CSharp.dll."
                );
            }

            foreach (var entry in enumGroup)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    throw new InvalidOperationException(
                        $"The server returned an enum entry with no name for '{enumGroup.Key}'."
                    );
                }

                if (enumType.Fields.Any(field => field.Name == entry.Name))
                {
                    throw new InvalidOperationException(
                        $"Enum '{enumGroup.Key}' already contains an entry named '{entry.Name}'."
                    );
                }

                if (
                    enumType.Fields.Any(field =>
                        field.HasConstant && Convert.ToInt64(field.Constant) == entry.Value
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"Enum '{enumGroup.Key}' already contains the value {entry.Value}."
                    );
                }

                enumType.Fields.Add(
                    CreateNewEnum(
                        ref assembly,
                        entry.AttributeName,
                        entry.Name,
                        enumType,
                        entry.Value
                    )
                );
            }
        }
    }

    private static void PatchSkillManager(ref AssemblyDefinition assembly)
    {
        var skillsExtendedModule = ModuleDefinition.ReadModule(PluginPath);
        var skillManagerExtendedType = skillsExtendedModule.GetType(
            "SkillsExtended.SkillsExtendedManager"
        );
        var skillManagerExtendedTypeRef = assembly.MainModule.ImportReference(
            skillManagerExtendedType
        );

        var skillManagerExtField = new FieldDefinition(
            "SkillsExtendedManager",
            FieldAttributes.Public,
            skillManagerExtendedTypeRef
        );

        _skillManager!.Fields.Add(skillManagerExtField);
    }
}

internal sealed class EnumEntryDefinition
{
    public string EnumType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AttributeName { get; set; }
    public int Value { get; set; }
}

internal sealed class LauncherConfig
{
    public string BackendUrl { get; set; } = string.Empty;
}
