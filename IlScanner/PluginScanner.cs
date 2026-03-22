using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IlScanner;

public enum Severity { Error, Warning }

public record Violation(Severity Severity, string Method, string Description);

public class PluginScanner
{
    private readonly HashSet<string> _blockedNamespaces;
    private readonly HashSet<string> _blockedTypes;
    private readonly HashSet<string> _blockedMethods;

    public PluginScanner(ScanPolicy policy)
    {
        _blockedNamespaces = policy.BlockedNamespaces.ToHashSet();
        _blockedTypes = policy.BlockedTypes.ToHashSet();
        _blockedMethods = policy.BlockedMethods.ToHashSet();
    }

    public List<Violation> Scan(string dllPath)
    {
        var violations = new List<Violation>();

        using var asm = AssemblyDefinition.ReadAssembly(dllPath,
            new ReaderParameters { ReadSymbols = false });

        foreach (var module in asm.Modules)
        {
            foreach (var type in module.GetTypes())
            {
                ScanTypeAttributes(type, violations);

                foreach (var method in type.Methods)
                {
                    string caller = $"{type.FullName}::{method.Name}";

                    if (method.IsPInvokeImpl || method.HasPInvokeInfo)
                    {
                        var lib = method.PInvokeInfo?.Module?.Name ?? "unknown";
                        violations.Add(new(Severity.Error, caller,
                            $"P/Invoke to native library: {lib}"));
                    }

                    if (!method.HasBody) continue;
                    ScanMethodBody(caller, method.Body, violations);
                }
            }
        }

        return violations;
    }

    private void ScanTypeAttributes(TypeDefinition type, List<Violation> violations)
    {
        if (type.IsExplicitLayout)
        {
            violations.Add(new(Severity.Warning, type.FullName,
                "Explicit layout can be used to reinterpret memory"));
        }
    }

    private void ScanMethodBody(string caller, MethodBody body, List<Violation> violations)
    {
        foreach (var instr in body.Instructions)
        {
            switch (instr.OpCode.Code)
            {
                case Code.Call:
                case Code.Callvirt:
                case Code.Newobj:
                case Code.Ldftn:
                case Code.Ldvirtftn:
                    if (instr.Operand is MethodReference target)
                        CheckMethodRef(caller, target, violations);
                    break;

                case Code.Calli:
                    violations.Add(new(Severity.Error, caller,
                        "calli instruction — indirect call bypasses API checks"));
                    break;

                case Code.Ldsfld:
                case Code.Stsfld:
                case Code.Ldfld:
                case Code.Stfld:
                    if (instr.Operand is FieldReference field)
                        CheckTypeRef(caller, field.DeclaringType, violations);
                    break;
            }
        }

        if (body.Method.IsUnsafe() || body.Method.IsPointerSignature())
        {
            violations.Add(new(Severity.Warning, caller,
                "Uses unsafe/pointer operations"));
        }
    }

    private void CheckMethodRef(string caller, MethodReference target, List<Violation> violations)
    {
        var declaringType = target.DeclaringType;
        string fullTypeName = declaringType.FullName;
        string ns = declaringType.Namespace;
        string fullMethodName = $"{fullTypeName}::{target.Name}";

        if (_blockedMethods.Contains(fullMethodName))
        {
            violations.Add(new(Severity.Error, caller,
                $"Calls blocked method: {fullMethodName}"));
            return;
        }

        if (_blockedTypes.Contains(fullTypeName))
        {
            violations.Add(new(Severity.Error, caller,
                $"Uses blocked type: {fullTypeName}"));
            return;
        }

        if (_blockedNamespaces.Any(blocked => ns == blocked || ns.StartsWith(blocked + ".")))
        {
            violations.Add(new(Severity.Error, caller,
                $"Uses blocked namespace: {ns} (via {fullMethodName})"));
        }
    }

    private void CheckTypeRef(string caller, TypeReference typeRef, List<Violation> violations)
    {
        string fullTypeName = typeRef.FullName;
        string ns = typeRef.Namespace;

        if (_blockedTypes.Contains(fullTypeName))
        {
            violations.Add(new(Severity.Error, caller,
                $"Accesses blocked type: {fullTypeName}"));
            return;
        }

        if (_blockedNamespaces.Any(blocked => ns == blocked || ns.StartsWith(blocked + ".")))
        {
            violations.Add(new(Severity.Error, caller,
                $"Accesses blocked namespace: {ns} (via {fullTypeName})"));
        }
    }
}

internal static class CecilExtensions
{
    public static bool IsUnsafe(this MethodDefinition method)
    {
        return method.Body?.Variables.Any(v => v.VariableType.IsPointer) == true
            || method.Parameters.Any(p => p.ParameterType.IsPointer);
    }

    public static bool IsPointerSignature(this MethodDefinition method)
    {
        return method.ReturnType.IsPointer
            || method.Parameters.Any(p => p.ParameterType.IsByReference && p.ParameterType.GetElementType().IsPointer);
    }
}
