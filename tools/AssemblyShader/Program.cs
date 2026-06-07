using Mono.Cecil;
using Mono.Cecil.Cil;

// Renames the unsigned 0Harmony/MonoMod assemblies to private identities so a
// "shaded" copy can coexist in the same AppDomain as UMM's bundled originals
// without a same-name/different-version clash. Mono.Cecil itself is left alone
// (it's strong-named and shared with UMM's copy).
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: AssemblyShader <sourceDir> <outputDir>");
    return 1;
}

var sourceDir = args[0];
var outputDir = args[1];
Directory.CreateDirectory(outputDir);

var rename = new Dictionary<string, string>
{
    ["0Harmony"] = "0Harmony.Melon",
    ["MonoMod.Utils"] = "MonoMod.Utils.Melon",
    ["MonoMod.RuntimeDetour"] = "MonoMod.RuntimeDetour.Melon",
};

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(sourceDir);
var readerParams = new ReaderParameters { AssemblyResolver = resolver, ReadSymbols = false };

foreach (var (oldName, newName) in rename)
{
    var srcPath = Path.Combine(sourceDir, oldName + ".dll");
    using var asm = AssemblyDefinition.ReadAssembly(srcPath, readerParams);

    asm.Name.Name = newName;

    foreach (var module in asm.Modules)
    {
        module.Name = newName + ".dll";

        foreach (var asmRef in module.AssemblyReferences)
        {
            if (rename.TryGetValue(asmRef.Name, out var renamedRef))
                asmRef.Name = renamedRef;
        }

        foreach (var type in module.GetTypes())
        {
            RewriteAttributeStrings(type.CustomAttributes, rename);
            foreach (var field in type.Fields)
                RewriteAttributeStrings(field.CustomAttributes, rename);
            foreach (var method in type.Methods)
            {
                RewriteAttributeStrings(method.CustomAttributes, rename);
                if (!method.HasBody) continue;
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string s && rename.TryGetValue(s, out var renamedStr))
                        instr.Operand = renamedStr;
                }
            }
        }
    }

    var outPath = Path.Combine(outputDir, newName + ".dll");
    asm.Write(outPath);
    Console.WriteLine($"{oldName}.dll -> {newName}.dll");
}

return 0;

static void RewriteAttributeStrings(IEnumerable<CustomAttribute> attributes, Dictionary<string, string> rename)
{
    foreach (var attr in attributes)
    {
        if (!attr.HasConstructorArguments) continue;
        for (int i = 0; i < attr.ConstructorArguments.Count; i++)
        {
            var arg = attr.ConstructorArguments[i];
            if (arg.Value is string s && rename.TryGetValue(s, out var renamed))
                attr.ConstructorArguments[i] = new CustomAttributeArgument(arg.Type, renamed);
        }
    }
}
