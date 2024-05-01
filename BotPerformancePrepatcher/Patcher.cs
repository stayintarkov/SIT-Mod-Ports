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
        (MethodDefinition updateByUnitySourceMethDef, MethodDefinition updateByUnityTargetMethDef, TypeDefinition botsClassTargetType, ILProcessor updateByUnitySourceIL) = SetupMethodForPatching(assembly, "UpdateByUnity", "Replacements", "BotsClass", "UpdateByUnity");
        (MethodDefinition method0SourceMethDef, MethodDefinition method0TargetMethDef, TypeDefinition aiTaskManagerTargetType, ILProcessor method0SourceIL) = SetupMethodForPatching(assembly, "method_0", "Replacements", "AITaskManager", "method_0");
        (MethodDefinition method1SourceMethDef, MethodDefinition method1TargetMethDef, TypeDefinition _, ILProcessor method1SourceIL) = SetupMethodForPatching(assembly, "method_1", "Replacements", "AITaskManager", "method_1");
        (MethodDefinition loadInternalSourceMethDef, MethodDefinition loadInternalTargetMethDef, TypeDefinition gClass537TargetType, ILProcessor loadInternalSourceIL) = SetupMethodForPatching(assembly, "LoadExternal", "Replacements", "GClass537", "LoadExternal");
        (MethodDefinition loadExternalSourceMethDef, MethodDefinition loadExternalTargetMethDef, TypeDefinition _, ILProcessor loadExternalSourceIL) = SetupMethodForPatching(assembly, "LoadInternal", "Replacements", "GClass537", "LoadInternal");
        (MethodDefinition loadSourceMethDef, MethodDefinition loadTargetMethDef, TypeDefinition _, ILProcessor loadSourceIL) = SetupMethodForPatching(assembly, "Load", "Replacements", "GClass537", "Load");
        (MethodDefinition saveSourceMethDef, MethodDefinition saveTargetMethDef, TypeDefinition _, ILProcessor saveSourceIL) = SetupMethodForPatching(assembly, "Save", "Replacements", "GClass537", "Save", true);
        (MethodDefinition updateSourceMethDef, MethodDefinition updateTargetMethDef, TypeDefinition aiCoreControllerCTargetType, ILProcessor updateSourceIL) = SetupMethodForPatching(assembly, "Update", "Replacements2", "AICoreControllerClass", "Update");

        
        botsClassTargetType.Module.ImportReference(updateByUnitySourceMethDef.DeclaringType.Fields.FirstOrDefault(f => f.Name == "hashSet_1")!.GetType());
        
        FixIL(updateByUnitySourceMethDef, botsClassTargetType, updateByUnitySourceIL);
        
        FixIL(method0SourceMethDef, aiTaskManagerTargetType, method0SourceIL);
        
        FixIL(method1SourceMethDef, aiTaskManagerTargetType, method1SourceIL);
        
        FixIL(loadInternalSourceMethDef, gClass537TargetType, loadInternalSourceIL);
        
        FixIL(loadExternalSourceMethDef, gClass537TargetType, loadExternalSourceIL);
        
        FixIL(loadSourceMethDef, gClass537TargetType, loadSourceIL);
        
        FixIL(saveSourceMethDef, gClass537TargetType, saveSourceIL);
        
        FixIL(updateSourceMethDef, aiCoreControllerCTargetType, updateSourceIL);

        updateByUnityTargetMethDef.Body = updateByUnitySourceMethDef.Body;
        method0TargetMethDef.Body = method0SourceMethDef.Body;
        method1TargetMethDef.Body = method1SourceMethDef.Body;
        loadInternalTargetMethDef.Body = loadInternalSourceMethDef.Body;
        loadExternalTargetMethDef.Body = loadExternalSourceMethDef.Body;
        loadTargetMethDef.Body = loadSourceMethDef.Body;
        saveTargetMethDef.Body = saveSourceMethDef.Body;
        updateTargetMethDef.Body = updateSourceMethDef.Body;
    }

    internal static void FixIL(MethodDefinition sourceMethod, TypeDefinition targetType, ILProcessor sourceIL)
    {
        if(sourceMethod == null) Console.WriteLine("SOMETHING IS WRONG");
        if(targetType == null) Console.WriteLine("SOMETHING IS WRONG 2");
        if(sourceIL == null) Console.WriteLine("SOMETHING IS WRONG 3");
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
                    try {
                        ci = sourceIL.Create(i.OpCode, targetType.Module.ImportReference(mref));
                    }
                    catch
                    {
                        Console.WriteLine("CAUGHT 3");
                    }
                }
            }
            else if(i.Operand is TypeReference tref){
                try
                {
                    ci = sourceIL.Create(i.OpCode, targetType.Module.ImportReference(tref));
                }
                catch
                {
                    Console.WriteLine("CAUGHT 2");
                }
            }
            else if (i.Operand is FieldReference && i.Previous.OpCode == OpCodes.Ldarg_0)
            {
                try
                {
                    ci = sourceIL.Create(i.OpCode, targetType.Fields.FirstOrDefault(f => f.Name == i.Operand.ToString().Split(':')[2]));
                }
                catch
                {
                    Console.WriteLine("CAUGHT");
                }
            }
            if(ci != i){
                sourceIL.Replace(i, ci);
            }
        }
        
        var vars = sourceMethod.Body.Variables.ToList();
        sourceMethod.Body.Variables.Clear();
        foreach(VariableDefinition v in vars)
        {
            var nv = new VariableDefinition(targetType.Module.ImportReference(v.VariableType));
            sourceMethod.Body.Variables.Add(nv);
        }
    }

    internal static (MethodDefinition, MethodDefinition, TypeDefinition, ILProcessor) SetupMethodForPatching(AssemblyDefinition assembly, string methodName, string sourceClass, string targetType, string targetMethod, bool specialCase = false)
    {
        MethodReference refs = assembly.MainModule.ImportReference(ReplacementsAssembly.MainModule.Types.FirstOrDefault(t => t.Name == sourceClass)!.Methods.FirstOrDefault(m => m.Name == methodName))!;
        TypeDefinition? targetTypeDef = assembly.MainModule.Types.FirstOrDefault(e => e.Name == targetType);
        if (targetTypeDef == null)
        {
            foreach (ModuleDefinition moduleDefinition in assembly.Modules)
            {
                targetTypeDef = moduleDefinition.Types.FirstOrDefault(t => t.Name == targetType);
                if (targetTypeDef != null) break;
            }

            if (targetTypeDef == null) throw new Exception("TYPE NOT FOUND");
        }
        MethodDefinition targetMethodDef = (specialCase) ? targetTypeDef!.Methods.FirstOrDefault(e => e.Name == targetMethod && e.Parameters.Count == 1)! : targetTypeDef!.Methods.FirstOrDefault(e => e.Name == targetMethod)!;
        return (refs.Resolve()!, targetMethodDef, targetTypeDef, refs.Resolve()!.Body.GetILProcessor());
    }
}