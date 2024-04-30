using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Runtime.InteropServices;

public static class Patcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};
    public static AssemblyDefinition ReplacementsAssembly = AssemblyDefinition.ReadAssembly("EscapeFromTarkov_Data\\Managed\\Replacements.dll");
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        (MethodDefinition updateByUnitySourceMethDef, MethodDefinition updateByUnityTargetMethDef, TypeDefinition botsClassTargetType, ILProcessor updateByUnitySourceIL) = SetupMethodForPatching(assembly, "UpdateByUnity", "BotsClass", "UpdateByUnity");
        (MethodDefinition method0SourceMethDef, MethodDefinition method0TargetMethDef, TypeDefinition aiTaskManagerTargetType, ILProcessor method0SourceIL) = SetupMethodForPatching(assembly, "method_0", "AITaskManager", "method_0");
        (MethodDefinition method1SourceMethDef, MethodDefinition method1TargetMethDef, TypeDefinition _, ILProcessor method1SourceIL) = SetupMethodForPatching(assembly, "method_1", "AITaskManager", "method_1");
        
        botsClassTargetType.Module.ImportReference(updateByUnitySourceMethDef.DeclaringType.Fields.FirstOrDefault(f => f.Name == "hashSet_1")!.GetType());
        aiTaskManagerTargetType.Module.ImportReference(aiTaskManagerTargetType.NestedTypes.FirstOrDefault(nt => nt.Name == "Data14"));
        
        FixIL(updateByUnitySourceMethDef, botsClassTargetType, updateByUnitySourceIL);
        
        FixIL(method0SourceMethDef, aiTaskManagerTargetType, method0SourceIL);
        
        FixIL(method1SourceMethDef, aiTaskManagerTargetType, method1SourceIL);
        
        updateByUnityTargetMethDef.Body = updateByUnitySourceMethDef.Body;
        method0TargetMethDef.Body = method0SourceMethDef.Body;
        method1TargetMethDef.Body = method1SourceMethDef.Body;
    }

    internal static void FixIL(MethodDefinition sourceMethod, TypeDefinition targetType, ILProcessor sourceIL)
    {
        foreach(var i in sourceMethod.Body.Instructions.ToList())
        {
            if (i == null) continue;
            var ci = i;
            if(i.Operand is MethodReference mref){
                if (mref.Name.Contains("AddFromList") && targetType.Name == "BotsClass")
                {
                    mref = targetType.Methods.FirstOrDefault(m => m.Name == "AddFromList")!;
                    ci = sourceIL.Create(i.OpCode, mref);
                }
                else
                {
                    ci = sourceIL.Create(i.OpCode, targetType.Module.ImportReference(mref));
                }
            }
            else if(i.Operand is TypeReference tref){
                ci = sourceIL.Create(i.OpCode, targetType.Module.ImportReference(tref));
            }
            else if (i.Operand is FieldReference && i.Previous.OpCode == OpCodes.Ldarg_0)
            {
                ci = sourceIL.Create(i.OpCode, targetType.Fields.FirstOrDefault(f => f.Name == i.Operand.ToString().Split(':')[2]));
            }
            if(ci != i){
                File.AppendAllText("I:\\OPS.txt", ci.Operand.ToString() + "\r\n");
                sourceIL.Replace(i, ci);
            }
        }
        
        var vars = sourceMethod.Body.Variables.ToList();
        sourceMethod.Body.Variables.Clear();
        foreach(VariableDefinition v in vars)
        {
            File.AppendAllText("I:\\NOPS.txt", v.ToString() + ":" + v.VariableType.ToString() + "\r\n");
            var nv = new VariableDefinition(targetType.Module.ImportReference(v.VariableType));
            sourceMethod.Body.Variables.Add(nv);
        }
    }

    internal static (MethodDefinition, MethodDefinition, TypeDefinition, ILProcessor) SetupMethodForPatching(AssemblyDefinition assembly, string methodName, string targetType, string targetMethod)
    {
        MethodReference refs = assembly.MainModule.ImportReference(ReplacementsAssembly.MainModule.Types.FirstOrDefault(t => t.Name == "Replacements")!.Methods.FirstOrDefault(m => m.Name == methodName))!;
        TypeDefinition targetTypeDef = assembly.MainModule.Types.FirstOrDefault(e => e.Name == targetType)!;
        MethodDefinition targetMethodDef = targetTypeDef!.Methods.FirstOrDefault(e => e.Name == targetMethod)!;
        return (refs.Resolve()!, targetMethodDef, targetTypeDef, refs.Resolve()!.Body.GetILProcessor());
    }
}