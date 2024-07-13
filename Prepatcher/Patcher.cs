using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Logging;
using System.Diagnostics;
using FieldAttributes = Mono.Cecil.FieldAttributes;

public static class Patcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new string[] { "Assembly-CSharp.dll" };

    public static TypeDefinition skillsClass;

    private static FieldDefinition CreateNewEnum(ref AssemblyDefinition assembly ,string AttributeName, string EnumName, TypeDefinition EnumClass, int CustomConstant)
    {
        var enumAttributeClass = assembly.MainModule.GetType("GAttribute21");
        
        var attributeConstructor = enumAttributeClass.Methods.First(m => m.IsConstructor);
        var valueArgument = new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, AttributeName);
        
        var attribute = new CustomAttribute(attributeConstructor);
        attribute.ConstructorArguments.Add(valueArgument);

        var newEnum = new FieldDefinition(EnumName, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, EnumClass) { Constant = CustomConstant };
        newEnum.CustomAttributes.Add(attribute);

        return newEnum;
    }

    private static void PatchNewBuffs(ref AssemblyDefinition assembly)
    {
        // New Buffs Enums
        var buffEnums = assembly.MainModule.GetType("EFT.EBuffId");
        var skillBuffClass = skillsClass.NestedTypes.FirstOrDefault(t => t.Name == "SkillBuff");

        var firstAidHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeed", 
            "FirstAidBuffHealingSpeed", 
            buffEnums, 
            1000);
        
        var firstAidHealingSpeedEliteEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeedElite", 
            "FirstAidBuffHealingSpeedElite", 
            buffEnums, 
            1001);
        
        var firstAidMaxHpEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidMaxHp", 
            "FirstAidMaxHp",
            buffEnums, 
            1002);
        
        var firstAidMaxHpEliteEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidMaxHpElite", 
            "FirstAidMaxHpElite", 
            buffEnums, 
            1003);
        
        buffEnums.Fields.Add(firstAidHealingSpeedEnum);
        buffEnums.Fields.Add(firstAidHealingSpeedEliteEnum);
        buffEnums.Fields.Add(firstAidMaxHpEnum);
        buffEnums.Fields.Add(firstAidMaxHpEliteEnum);
        
        var fieldMedicineHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSpeed", 
            "FieldMedicineSpeed", 
            buffEnums, 
            1004);
        
        var fieldMedicineHealingSpeedEliteEnum = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSpeedElite", 
            "FieldMedicineBuffSpeedElite", 
            buffEnums, 
            1005);
        
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEnum);
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEliteEnum);

        var usecArSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoil", 
            "UsecArSystemsRecoil", 
            buffEnums, 
            1006);
        
        var usecArSystemsRecoilEliteEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoilElite", 
            "UsecArSystemsRecoilElite", 
            buffEnums, 
            1007);
        
        var usecArSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgo", 
            "UsecArSystemsErgo", 
            buffEnums, 
            1008);
        
        var usecArSystemsErgoEliteEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgoElite", 
            "UsecArSystemsErgoElite", 
            buffEnums, 
            1009);
        
        buffEnums.Fields.Add(usecArSystemsRecoilEnum);
        buffEnums.Fields.Add(usecArSystemsRecoilEliteEnum);
        buffEnums.Fields.Add(usecArSystemsErgoEnum);
        buffEnums.Fields.Add(usecArSystemsErgoEliteEnum);
        
        var bearAkSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "BearArSystemsRecoil", 
            "BearArSystemsRecoil", 
            buffEnums, 
            1010);
        
        var bearAkSystemsRecoilEliteEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsRecoilElite", 
            "BearAkSystemsRecoilElite", 
            buffEnums, 
            1011);
        
        var bearAkSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgo", 
            "BearAkSystemsErgo", 
            buffEnums, 
            1012);
        
        var bearAkSystemsErgoEliteEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgoElite", 
            "BearAkSystemsErgoElite", 
            buffEnums, 
            1013);
        
        buffEnums.Fields.Add(bearAkSystemsRecoilEnum);
        buffEnums.Fields.Add(bearAkSystemsRecoilEliteEnum);
        buffEnums.Fields.Add(bearAkSystemsErgoEnum);
        buffEnums.Fields.Add(bearAkSystemsErgoEliteEnum);
        
        var lockpickingTimeReduction = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeRed", 
            "LockpickingTimeRed", 
            buffEnums, 
            1013);
        
        var lockpickingTimeReductionElite = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeRedElite", 
            "LockpickingTimeRedElite", 
            buffEnums, 
            1014);
        
        buffEnums.Fields.Add(lockpickingTimeReduction);
        buffEnums.Fields.Add(lockpickingTimeReductionElite);
    }

    private static void AddSkillFields(ref AssemblyDefinition assembly)
    {
        return;
    }
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        try
        {
            //Set Global Vars
            skillsClass = assembly.MainModule.GetType("EFT.SkillManager");

            AddSkillFields(ref assembly);
            PatchNewBuffs(ref assembly);

            Logger.CreateLogSource("Skills Extended Prepatcher").LogInfo("Patching Complete!");
        } catch (Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            Logger.CreateLogSource("Skills Extended Prepatcher").LogInfo("Error When Patching: " + ex.Message + " - Line " + line);
        }
    }
}