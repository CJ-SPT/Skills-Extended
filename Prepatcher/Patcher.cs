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
        TypeDefinition enumAttributeClass = assembly.MainModule.GetType("GAttribute19");
        MethodReference attributeConstructor = enumAttributeClass.Methods.First(m => m.IsConstructor);
        CustomAttributeArgument valueArgument = new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, AttributeName);

        CustomAttribute attribute = new CustomAttribute(attributeConstructor);
        attribute.ConstructorArguments.Add(valueArgument);

        var newEnum = new FieldDefinition(EnumName, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, EnumClass) { Constant = CustomConstant };
        newEnum.CustomAttributes.Add(attribute);

        return newEnum;
    }

    private static void AddskillFields(ref AssemblyDefinition assembly)
    {
        TypeDefinition skillEnums = assembly.MainModule.GetType("EFT.ESkillId");

        FieldDefinition bearAKSystems = CreateNewEnum(ref assembly, "BearAKSystems", "SkillsExtended", skillEnums, 1000);
        FieldDefinition usecARSystems = CreateNewEnum(ref assembly, "UsecARSystems", "SkillsExtended", skillEnums, 1001);

        skillEnums.Fields.Add(bearAKSystems);
        skillEnums.Fields.Add(usecARSystems);

        TypeDefinition GClass1635 = assembly.MainModule.GetType("GClass1635");

        FieldDefinition bearVar = new FieldDefinition("BearAKSystems", FieldAttributes.Public, GClass1635);
        FieldDefinition usecVar = new FieldDefinition("UsecARSystems", FieldAttributes.Public, GClass1635);

        skillsClass.Fields.Add(bearVar);
        skillsClass.Fields.Add(usecVar);
    }
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        try
        {
            //Set Global Vars
            skillsClass = assembly.MainModule.GetType("EFT.SkillManager");

            AddskillFields(ref assembly);

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