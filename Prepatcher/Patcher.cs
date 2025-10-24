using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Logging;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using FieldAttributes = Mono.Cecil.FieldAttributes;

public static class SkillsExtendedPatcher
{
    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];
    private static TypeDefinition _skillManager;

    private static readonly string PatcherPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

#if DEBUG
    private static readonly string DumpedAsmPath = Path.Combine(PatcherPath, "..", "DumpedAssemblies", "EscapeFromTarkov");
#endif

    private static readonly string PluginPath = Path.Combine(PatcherPath, "..", "plugins", "SkillsExtended", "SkillsExtended.dll");
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        try
        {
            _skillManager = assembly.MainModule.GetType("EFT.SkillManager");
            
            PatchNewBuffs(ref assembly);
            PatchSkillManager(ref assembly);
            
            Logger.CreateLogSource("Skills Extended PrePatch").LogInfo("Patching Complete!");

#if DEBUG
            if (!Directory.Exists(DumpedAsmPath))
                Directory.CreateDirectory(DumpedAsmPath);
            var dumpPath = Path.Combine(DumpedAsmPath, "Assembly-CSharp.dll");
            assembly.Write(dumpPath);
            Logger.CreateLogSource("Skills Extended PrePatch").LogInfo("Dumped Patched Assembly to: " + dumpPath);
#endif

        } catch (Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            Logger.CreateLogSource("Skills Extended PrePatch").LogError("Error When Patching: " + ex.Message + " - Line " + line);
        }
    }
    
    private static FieldDefinition CreateNewEnum(ref AssemblyDefinition assembly, [CanBeNull] string AttributeName, string EnumName, TypeDefinition EnumClass, int CustomConstant)
    {
        var enumAttributeClass = assembly.MainModule.GetType("GAttribute24");
        
        var attributeConstructor = enumAttributeClass.Methods.First(m => m.IsConstructor);
        
        var attribute = new CustomAttribute(attributeConstructor);

        if (AttributeName is not null)
        {
            var valueArgument = new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, AttributeName);
            attribute.ConstructorArguments.Add(valueArgument);
        }
        
        var newEnum = new FieldDefinition(
            EnumName, 
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, EnumClass) 
            { Constant = CustomConstant };
        
        newEnum.CustomAttributes.Add(attribute);

        return newEnum;
    }

    private static void PatchNewBuffs(ref AssemblyDefinition assembly)
    {
        // New Buffs Enums
        var buffEnums = assembly.MainModule.GetType("EFT.EBuffId");
        var index = 1000;
        
        // New skills
        FirstAidBuffs(assembly, buffEnums, ref index);
        FieldMedicineBuffs(assembly, buffEnums, ref index);
        WesternRifleBuffs(assembly, buffEnums, ref index);
        EasternRifleBuffs(assembly, buffEnums, ref index);
        LockPickingBuffs(assembly, buffEnums, ref index);
        SilentOpsBuffs(assembly, buffEnums, ref index);
        
        // Existing skills
        StrengthBuffs(assembly, buffEnums, ref index);
        EnduranceBuffs(assembly, buffEnums, ref index);
        VitalityBuffs(assembly, buffEnums, ref index);
        HealthBuffs(assembly, buffEnums, ref index);
	}

    private static void FirstAidBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var firstAidHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeed", 
            "FirstAidHealingSpeed", 
            buffEnum, 
            index++);
        
        var firstAidHealingCostEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidResourceCost", 
            "FirstAidResourceCost",
            buffEnum, 
            index++);
        
        var firstAidMovementSpeedElite = CreateNewEnum(
            ref assembly, 
            "FirstAidMovementSpeedElite", 
            "FirstAidMovementSpeedElite",
            buffEnum, 
            index++);
        
        buffEnum.Fields.Add(firstAidHealingSpeedEnum);
        buffEnum.Fields.Add(firstAidHealingCostEnum);
        buffEnum.Fields.Add(firstAidMovementSpeedElite);
    }

    private static void FieldMedicineBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var fieldMedicineSkillCap = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSkillCap", 
            "FieldMedicineSkillCap", 
            buffEnum, 
            index++);
        
        var fieldMedicineDurationBonus = CreateNewEnum(
            ref assembly, 
            "FieldMedicineDurationBonus", 
            "FieldMedicineDurationBonus", 
            buffEnum, 
            index++);
        
        var fieldMedicineChanceBonus = CreateNewEnum(
            ref assembly, 
            "FieldMedicineChanceBonus", 
            "FieldMedicineChanceBonus", 
            buffEnum, 
            index++);
        
        buffEnum.Fields.Add(fieldMedicineSkillCap);
        buffEnum.Fields.Add(fieldMedicineDurationBonus);
        buffEnum.Fields.Add(fieldMedicineChanceBonus);
    }

    private static void WesternRifleBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var usecArSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoil", 
            "UsecArSystemsRecoil", 
            buffEnum, 
            index++);
        
        var usecArSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgo", 
            "UsecArSystemsErgo", 
            buffEnum, 
            index++);
        
        
        buffEnum.Fields.Add(usecArSystemsRecoilEnum);
        buffEnum.Fields.Add(usecArSystemsErgoEnum);
    }

    private static void EasternRifleBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var bearAkSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsRecoil", 
            "BearAkSystemsRecoil", 
            buffEnum, 
            index++);
        
        var bearAkSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgo", 
            "BearAkSystemsErgo", 
            buffEnum, 
            index++);
        
        buffEnum.Fields.Add(bearAkSystemsRecoilEnum);
        buffEnum.Fields.Add(bearAkSystemsErgoEnum);
    }

    private static void LockPickingBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var lockpickingTimeIncrease = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeIncrease", 
            "LockpickingTimeIncrease", 
            buffEnum, 
            index++);
        
        var lockpickingForgivenessAngle = CreateNewEnum(
            ref assembly, 
            "LockpickingForgivenessAngle", 
            "LockpickingForgivenessAngle", 
            buffEnum, 
            index++);
        
        var lockpickingUseElite = CreateNewEnum(
            ref assembly, 
            "LockpickingUseElite", 
            "LockpickingUseElite", 
            buffEnum, 
            index++);
        
        buffEnum.Fields.Add(lockpickingTimeIncrease);
        buffEnum.Fields.Add(lockpickingForgivenessAngle);
        buffEnum.Fields.Add(lockpickingUseElite);
    }

    private static void SilentOpsBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var silentOpsIncMeleeSpeed = CreateNewEnum(
            ref assembly, 
            "SilentOpsIncMeleeSpeed", 
            "SilentOpsIncMeleeSpeed", 
            buffEnum, 
            index++);
        
        var silentOpsRedVolume = CreateNewEnum(
            ref assembly, 
            "SilentOpsRedVolume", 
            "SilentOpsRedVolume", 
            buffEnum, 
            index++);
        
        var silentOpsSilencerCostRed = CreateNewEnum(
            ref assembly, 
            "SilentOpsSilencerCostRed", 
            "SilentOpsSilencerCostRed", 
            buffEnum, 
            index++);
        
        buffEnum.Fields.Add(silentOpsIncMeleeSpeed);
        buffEnum.Fields.Add(silentOpsRedVolume);
        buffEnum.Fields.Add(silentOpsSilencerCostRed);
    }

    private static void StrengthBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var incBushSpeed = CreateNewEnum(
            ref assembly, 
            "StrengthColliderSpeedBuff", 
            "StrengthColliderSpeedBuff", 
            buffEnum, 
            index++);
        
        var incBushSpeedElite = CreateNewEnum(
            ref assembly, 
            "StrengthColliderSpeedBuffElite", 
            "StrengthColliderSpeedBuffElite", 
            buffEnum, 
            index++);

        var strArmsHP = CreateNewEnum(
            ref assembly, 
            "StrengthArmsHPBuff", 
            "StrengthArmsHPBuff", 
            buffEnum, 
            index++);

        var strArmsHPElite = CreateNewEnum(
            ref assembly, 
            "StrengthArmsHPBuffElite", 
            "StrengthArmsHPBuffElite", 
            buffEnum, 
            index++);

        buffEnum.Fields.Add(incBushSpeed);
        buffEnum.Fields.Add(incBushSpeedElite);
        buffEnum.Fields.Add(strArmsHP);
        buffEnum.Fields.Add(strArmsHPElite);
    }

    private static void EnduranceBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var endLegsHP = CreateNewEnum(
            ref assembly, 
            "EnduranceLegsHPBuff", 
            "EnduranceLegsHPBuff", 
            buffEnum, 
            index++);

        var endLegsHPElite = CreateNewEnum(
            ref assembly, 
            "EnduranceLegsHPBuffElite", 
            "EnduranceLegsHPBuffElite", 
            buffEnum, 
            index++);

        buffEnum.Fields.Add(endLegsHP);
        buffEnum.Fields.Add(endLegsHPElite);
	}

    private static void VitalityBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var vitTorsoHP = CreateNewEnum(
            ref assembly, 
            "VitalityTorsoHPBuff", 
            "VitalityTorsoHPBuff", 
            buffEnum, 
            index++);

        var vitTorsoHPElite = CreateNewEnum(
            ref assembly, 
            "VitalityTorsoHPBuffElite", 
            "VitalityTorsoHPBuffElite", 
            buffEnum, 
            index++);

        buffEnum.Fields.Add(vitTorsoHP);
        buffEnum.Fields.Add(vitTorsoHPElite);
	}

    private static void HealthBuffs(AssemblyDefinition assembly, TypeDefinition buffEnum, ref int index)
    {
        var hltHeadHP = CreateNewEnum(
            ref assembly,
            "HealthHeadHPBuff",
            "HealthHeadHPBuff",
            buffEnum,
            index++);

        var hltHeadHPElite = CreateNewEnum(
            ref assembly,
            "HealthHeadHPBuffElite",
            "HealthHeadHPBuffElite",
            buffEnum,
            index++);

        buffEnum.Fields.Add(hltHeadHP);
        buffEnum.Fields.Add(hltHeadHPElite);
    }

	private static void PatchSkillManager(ref AssemblyDefinition assembly)
    {
        var skillsExtendedModule = ModuleDefinition.ReadModule(PluginPath);
        var skillManagerExtendedType = skillsExtendedModule.GetType("SkillsExtended.Skills.Core.SkillManagerExt");
        var skillManagerExtendedTypeRef = assembly.MainModule.ImportReference(skillManagerExtendedType);
        
        var skillManagerExtField = new FieldDefinition(
            "SkillManagerExtended", 
            FieldAttributes.Public, 
            skillManagerExtendedTypeRef);
        
        _skillManager.Fields.Add(skillManagerExtField);
    }
}