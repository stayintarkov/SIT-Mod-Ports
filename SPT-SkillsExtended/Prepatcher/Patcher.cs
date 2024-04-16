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

    private static void PatchNewBuffs(ref AssemblyDefinition assembly)
    {
        // New Buffs Enums
        TypeDefinition buffEnums = assembly.MainModule.GetType("EFT.EBuffId");
        TypeDefinition gClass1641 = skillsClass.NestedTypes.FirstOrDefault(t => t.Name == "GClass1641");

        FieldDefinition firstAidhealingSpeedEnum = CreateNewEnum(ref assembly, "FirstaidBuffHealingSpeed", "FirstaidBuffHealingSpeed", buffEnums, 1000);
        FieldDefinition firstAidhealingSpeedEliteEnum = CreateNewEnum(ref assembly, "FirstaidBuffHealingSpeedElite", "FirstaidBuffHealingSpeedElite", buffEnums, 1001);
        buffEnums.Fields.Add(firstAidhealingSpeedEnum);
        buffEnums.Fields.Add(firstAidhealingSpeedEliteEnum);

        FieldDefinition firstAidmaxHpEnum = CreateNewEnum(ref assembly, "FirstaidBuffMaxHp", "FirstaidBuffMaxHp", buffEnums, 1002);
        FieldDefinition firstAidmaxHpEliteEnum = CreateNewEnum(ref assembly, "FirstaidBuffMaxHpElite", "FirstaidBuffMaxHpElite", buffEnums, 1003);
        buffEnums.Fields.Add(firstAidmaxHpEnum);
        buffEnums.Fields.Add(firstAidmaxHpEliteEnum);

        FieldDefinition fieldMedicineHealingSpeedEnum = CreateNewEnum(ref assembly, "FieldMedicineBuffSpeed", "FieldMedicineBuffSpeed", buffEnums, 1004);
        FieldDefinition fieldMedicineHealingSpeedEliteEnum = CreateNewEnum(ref assembly, "FieldMedicineBuffSpeedElite", "FieldMedicineBuffSpeedElite", buffEnums, 1005);
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEnum);
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEliteEnum);

        // New Buff Vars

        FieldDefinition firstAidhealingSpeedBuffVar = new FieldDefinition("FirstaidBuffHealingSpeed", FieldAttributes.Public, gClass1641);
        FieldDefinition firstAidhealingSpeedEliteBuffVar = new FieldDefinition("FirstaidBuffHealingSpeedElite", FieldAttributes.Public, gClass1641);
        skillsClass.Fields.Add(firstAidhealingSpeedBuffVar);
        skillsClass.Fields.Add(firstAidhealingSpeedEliteBuffVar);

        FieldDefinition firstAidmaxHpBuffVar = new FieldDefinition("FirstaidBuffMaxHp", FieldAttributes.Public, gClass1641);
        FieldDefinition firstAidmaxHpEliteBuffVar = new FieldDefinition("FirstaidBuffMaxHp", FieldAttributes.Public, gClass1641);    
        skillsClass.Fields.Add(firstAidmaxHpBuffVar);
        skillsClass.Fields.Add(firstAidmaxHpBuffVar);

        FieldDefinition fieldMedicineHealingSpeedBuffVar = new FieldDefinition("FieldMedicineBuffSpeed", FieldAttributes.Public, gClass1641);
        FieldDefinition fieldMedicineHealingSpeedEliteBuffVar = new FieldDefinition("FieldMedicineBuffSpeedElite", FieldAttributes.Public, gClass1641);
        skillsClass.Fields.Add(fieldMedicineHealingSpeedBuffVar);
        skillsClass.Fields.Add(fieldMedicineHealingSpeedEliteBuffVar);
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