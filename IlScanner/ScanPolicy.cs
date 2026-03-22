namespace IlScanner;

public class ScanPolicy
{
    public List<string> BlockedNamespaces { get; init; } = [];
    public List<string> BlockedTypes { get; init; } = [];
    public List<string> BlockedMethods { get; init; } = [];

    /// <summary>
    /// A reasonable default for game editor plugins:
    /// block file I/O, networking, process control, reflection emit, and P/Invoke.
    /// </summary>
    public static ScanPolicy GameEditorDefault => new()
    {
        BlockedNamespaces =
        [
            "System.IO",
            "System.Net",
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Diagnostics",
            "System.Reflection.Emit",
            "System.Runtime.InteropServices",
            "System.Runtime.Loader",
            "System.Security.Cryptography",
            "System.Threading",
        ],
        BlockedTypes =
        [
            "System.Reflection.Assembly",
            "System.Reflection.MethodBase",
            "System.Activator",
            "System.AppDomain",
            "System.Environment",
            "System.GC",
            "System.Runtime.CompilerServices.Unsafe",
        ],
        BlockedMethods =
        [
            "System.Type::InvokeMember",
        ],
    };
}
