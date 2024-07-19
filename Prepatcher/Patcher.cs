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
        var skillBuffClass = skillsClass.NestedTypes.FirstOrDefault(t => t.Name == "SkillBuff");

        var firstAidHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeed", 
            "FirstAidHealingSpeed", 
            buffEnums, 
            1000);
        
        var firstAidMaxHpEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidMaxHp", 
            "FirstAidMaxHp",
            buffEnums, 
            1001);
        
        buffEnums.Fields.Add(firstAidHealingSpeedEnum);
        buffEnums.Fields.Add(firstAidMaxHpEnum);
        
        var fieldMedicineHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSpeed", 
            "FieldMedicineSpeed", 
            buffEnums, 
            1002);
        
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEnum);

        var usecArSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoil", 
            "UsecArSystemsRecoil", 
            buffEnums, 
            1003);
        
        var usecArSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgo", 
            "UsecArSystemsErgo", 
            buffEnums, 
            1004);
        
        
        buffEnums.Fields.Add(usecArSystemsRecoilEnum);
        buffEnums.Fields.Add(usecArSystemsErgoEnum);
        
        var bearAkSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsRecoil", 
            "BearAkSystemsRecoil", 
            buffEnums, 
            1005);
        
        var bearAkSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgo", 
            "BearAkSystemsErgo", 
            buffEnums, 
            1006);
        
        buffEnums.Fields.Add(bearAkSystemsRecoilEnum);
        buffEnums.Fields.Add(bearAkSystemsErgoEnum);
        
        var lockpickingTimeReduction = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeReduction", 
            "LockpickingTimeReduction", 
            buffEnums, 
            1007);
        
        var lockpickingUseElite = CreateNewEnum(
            ref assembly, 
            "LockpickingUseElite", 
            "LockpickingUseElite", 
            buffEnums, 
            1008);
        
        buffEnums.Fields.Add(lockpickingTimeReduction);
        buffEnums.Fields.Add(lockpickingUseElite);
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