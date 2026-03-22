using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GamePlugin;

public static class Plugin
{
    private static unsafe delegate* unmanaged[Stdcall]<IntPtr, void> s_engineLog;
    private static unsafe delegate* unmanaged[Stdcall]<int> s_engineGetFrame;

    [UnmanagedCallersOnly]
    public static unsafe void Init(void* logFn, void* getFrameFn)
    {
        s_engineLog = (delegate* unmanaged[Stdcall]<IntPtr, void>)logFn;
        s_engineGetFrame = (delegate* unmanaged[Stdcall]<int>)getFrameFn;

        EngineLog("GamePlugin.Init called — managed side is alive!");

        string runtime = RuntimeInformation.FrameworkDescription;
        string interpMode = Environment.GetEnvironmentVariable("DOTNET_InterpMode") ?? "(not set)";
        EngineLog($"  Runtime: {runtime}");
        EngineLog($"  InterpMode: {interpMode}");
    }

    [UnmanagedCallersOnly]
    public static unsafe int Update()
    {
        int frame = s_engineGetFrame();
        EngineLog($"Update tick — frame {frame}, computing 100+{frame} = {100 + frame}");
        return 0;
    }

    [UnmanagedCallersOnly]
    public static void RunBenchmark()
    {
        const int n = 1_000_000;

        EngineLog($"Running benchmarks (n={n})...");

        // Dict lookup
        const int keyCount = 10_000;
        var keys = new string[keyCount];
        for (int i = 0; i < keyCount; i++)
            keys[i] = "k" + i.ToString();
        var dict = new Dictionary<string, int>(keyCount, StringComparer.Ordinal);
        for (int i = 0; i < keyCount; i++)
            dict[keys[i]] = i;

        var sw = Stopwatch.StartNew();
        int sum = 0;
        for (int i = 0; i < n; i++)
            sum += dict[keys[i % keyCount]];
        sw.Stop();
        EngineLog($"  dict lookup      : {sw.Elapsed.TotalMilliseconds,8:F2} ms (checksum {sum})");

        // Math loop
        sw.Restart();
        double x = 1.0;
        for (int i = 0; i < n; i++)
            x = (x * 1.0000001 + i * 0.0000001) % 97.0;
        sw.Stop();
        EngineLog($"  math loop        : {sw.Elapsed.TotalMilliseconds,8:F2} ms (sink {x:F6})");

        // Interface dispatch
        IAnimal dog = new Dog();
        IAnimal cat = new Cat();
        sw.Restart();
        int v = 0;
        for (int i = 0; i < n; i++)
            v += ((i & 1) == 0 ? dog : cat).Speak();
        sw.Stop();
        EngineLog($"  interface dispatch: {sw.Elapsed.TotalMilliseconds,8:F2} ms (sink {v})");

        // Cross-call to native (engine_get_frame)
        sw.Restart();
        int f = 0;
        for (int i = 0; i < n; i++)
            f = CallEngineGetFrame();
        sw.Stop();
        double nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / n;
        EngineLog($"  native cross-call: {sw.Elapsed.TotalMilliseconds,8:F2} ms ({nsPerCall:F0} ns/call, frame={f})");

        EngineLog("Benchmarks complete.");
    }

    private static unsafe void EngineLog(string message)
    {
        fixed (char* p = message)
        {
            s_engineLog((IntPtr)p);
        }
    }

    private static unsafe int CallEngineGetFrame()
    {
        return s_engineGetFrame();
    }
}

internal interface IAnimal { int Speak(); }
internal class Dog : IAnimal { public int Speak() => 1; }
internal class Cat : IAnimal { public int Speak() => 2; }
