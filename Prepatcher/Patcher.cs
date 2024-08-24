using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Logging;
using System.Diagnostics;
using JetBrains.Annotations;
using FieldAttributes = Mono.Cecil.FieldAttributes;

public static class Patcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new string[] { "Assembly-CSharp.dll" };
    public static TypeDefinition SkillManager;
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        try
        {
            SkillManager = assembly.MainModule.GetType("EFT.SkillManager");
            
            PatchNewBuffs(ref assembly);
            PatchNewAnim(ref assembly);
            PatchSkillManagerSide(ref assembly);
            
            Logger.CreateLogSource("Skills Extended PrePatch").LogInfo("Patching Complete!");
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
        var enumAttributeClass = assembly.MainModule.GetType("GAttribute21");
        
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
        
        var firstAidHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeed", 
            "FirstAidHealingSpeed", 
            buffEnums, 
            index++);
        
        var firstAidHealingCostEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidResourceCost", 
            "FirstAidResourceCost",
            buffEnums, 
            index++);
        
        
        var firstAidMovementSpeedElite = CreateNewEnum(
            ref assembly, 
            "FirstAidMovementSpeedElite", 
            "FirstAidMovementSpeedElite",
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(firstAidHealingSpeedEnum);
        buffEnums.Fields.Add(firstAidHealingCostEnum);
        buffEnums.Fields.Add(firstAidMovementSpeedElite);
        
        var fieldMedicineSkillCap = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSkillCap", 
            "FieldMedicineSkillCap", 
            buffEnums, 
            index++);
        
        var fieldMedicineDurationBonus = CreateNewEnum(
            ref assembly, 
            "FieldMedicineDurationBonus", 
            "FieldMedicineDurationBonus", 
            buffEnums, 
            index++);
        
        var fieldMedicineChanceBonus = CreateNewEnum(
            ref assembly, 
            "FieldMedicineChanceBonus", 
            "FieldMedicineChanceBonus", 
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(fieldMedicineSkillCap);
        buffEnums.Fields.Add(fieldMedicineDurationBonus);
        buffEnums.Fields.Add(fieldMedicineChanceBonus);
        
        var usecArSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoil", 
            "UsecArSystemsRecoil", 
            buffEnums, 
            index++);
        
        var usecArSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgo", 
            "UsecArSystemsErgo", 
            buffEnums, 
            index++);
        
        
        buffEnums.Fields.Add(usecArSystemsRecoilEnum);
        buffEnums.Fields.Add(usecArSystemsErgoEnum);
        
        var bearAkSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsRecoil", 
            "BearAkSystemsRecoil", 
            buffEnums, 
            index++);
        
        var bearAkSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgo", 
            "BearAkSystemsErgo", 
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(bearAkSystemsRecoilEnum);
        buffEnums.Fields.Add(bearAkSystemsErgoEnum);
        
        var lockpickingTimeIncrease = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeIncrease", 
            "LockpickingTimeIncrease", 
            buffEnums, 
            index++);
        
        var lockpickingForgivenessAngle = CreateNewEnum(
            ref assembly, 
            "LockpickingForgivenessAngle", 
            "LockpickingForgivenessAngle", 
            buffEnums, 
            index++);
        
        var lockpickingUseElite = CreateNewEnum(
            ref assembly, 
            "LockpickingUseElite", 
            "LockpickingUseElite", 
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(lockpickingTimeIncrease);
        buffEnums.Fields.Add(lockpickingForgivenessAngle);
        buffEnums.Fields.Add(lockpickingUseElite);
        
        var bearAuthorityTraderPriceRed = CreateNewEnum(
            ref assembly, 
            "BearAuthorityTraderPriceRed", 
            "BearAuthorityTraderPriceRed", 
            buffEnums, 
            index++);
        
        var bearAuthorityRepairPriceRed = CreateNewEnum(
            ref assembly, 
            "BearAuthorityRepairPriceRed", 
            "BearAuthorityRepairPriceRed", 
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(bearAuthorityTraderPriceRed);
        buffEnums.Fields.Add(bearAuthorityRepairPriceRed);
        
        var usecNegotiationTraderPriceRed = CreateNewEnum(
            ref assembly, 
            "UsecNegotiationsTraderPriceRed", 
            "UsecNegotiationsTraderPriceRed", 
            buffEnums, 
            index++);
        
        var usecNegotiationHealingPriceRed = CreateNewEnum(
            ref assembly, 
            "UsecNegotiationsHealingPriceRed", 
            "UsecNegotiationsHealingPriceRed", 
            buffEnums, 
            index++);
        
        buffEnums.Fields.Add(usecNegotiationTraderPriceRed);
        buffEnums.Fields.Add(usecNegotiationHealingPriceRed);
    }

    private static void PatchNewAnim(ref AssemblyDefinition assembly)
    {
        var animEnum = assembly.MainModule.GetType("EFT.InputSystem.ECommand");
        
        var lockPickingAnimStart = CreateNewEnum(
            ref assembly, 
            null, 
            "LockPickingStart", 
            animEnum, 
            1000);
        
        var lockPickingAnimEnd = CreateNewEnum(
            ref assembly, 
            null, 
            "LockPickingEnd", 
            animEnum, 
            1001);
        
        animEnum.Fields.Add(lockPickingAnimStart);
        animEnum.Fields.Add(lockPickingAnimEnd);
    }

    private static void PatchSkillManagerSide(ref AssemblyDefinition assembly)
    {
        var ePlayerSide = assembly.MainModule.GetType("EFT.EPlayerSide");
        
        // This is the intermediate object the skill manager gets deserialized onto
        var jsonObj = assembly.MainModule.GetType("GClass1795");

        var mainModule = assembly.MainModule;
        
        var sideField = new FieldDefinition(
            "Side", 
            FieldAttributes.Public, 
            ePlayerSide);
        
        var sideFieldJson = new FieldDefinition(
            "Side", 
            FieldAttributes.Public, 
            ePlayerSide);
        
        SkillManager.Fields.Add(sideField);
        jsonObj.Fields.Add(sideFieldJson);
        
        var isPlayerField = new FieldDefinition(
            "IsYourPlayer", 
            FieldAttributes.Public, 
            mainModule.ImportReference(typeof(bool)));

        isPlayerField.Constant = false;
        
        var isPlayerFieldJson = new FieldDefinition(
            "IsYourPlayer", 
            FieldAttributes.Public, 
            mainModule.ImportReference(typeof(bool)));
        
        isPlayerFieldJson.Constant = false;
        
        SkillManager.Fields.Add(isPlayerField);
        jsonObj.Fields.Add(isPlayerFieldJson);
    }
}