using System.Runtime.InteropServices;
using PluginApi;

namespace GameEngine;

public static class EngineHost
{
    private static PluginManager? s_pluginManager;

    [UnmanagedCallersOnly]
    public static unsafe void Init(void* logFn, void* getFrameFn)
    {
        var services = new NativeEngineServices(
            (delegate* unmanaged[Stdcall]<IntPtr, void>)logFn,
            (delegate* unmanaged[Stdcall]<int>)getFrameFn);

        s_pluginManager = new PluginManager(services);
        services.Log("GameEngine initialized");
    }

    [UnmanagedCallersOnly]
    public static void LoadPlugin(IntPtr pathPtr)
    {
        string path = Marshal.PtrToStringUni(pathPtr)!;
        s_pluginManager!.LoadPlugin(path);
    }

    [UnmanagedCallersOnly]
    public static void Tick()
    {
        s_pluginManager!.UpdateAll(0.016f);
    }

    [UnmanagedCallersOnly]
    public static void Shutdown()
    {
        s_pluginManager!.UnloadAll();
    }
}

internal unsafe class NativeEngineServices : IEngineServices
{
    private readonly delegate* unmanaged[Stdcall]<IntPtr, void> _log;
    private readonly delegate* unmanaged[Stdcall]<int> _getFrame;

    public NativeEngineServices(
        delegate* unmanaged[Stdcall]<IntPtr, void> log,
        delegate* unmanaged[Stdcall]<int> getFrame)
    {
        _log = log;
        _getFrame = getFrame;
    }

    public void Log(string message)
    {
        fixed (char* p = message)
            _log((IntPtr)p);
    }

    public int GetFrame() => _getFrame();
}
