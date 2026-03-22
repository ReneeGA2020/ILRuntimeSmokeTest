using System.Reflection;
using System.Runtime.Loader;
using IlScanner;
using PluginApi;

namespace GameEngine;

internal class PluginManager
{
    private readonly IEngineServices _services;
    private readonly PluginScanner _scanner;
    private readonly List<LoadedPlugin> _plugins = [];

    public PluginManager(IEngineServices services)
    {
        _services = services;
        _scanner = new PluginScanner(ScanPolicy.GameEditorDefault);
    }

    public void LoadPlugin(string dllPath)
    {
        dllPath = Path.GetFullPath(dllPath);
        _services.Log("Loading plugin: " + dllPath);

        var violations = _scanner.Scan(dllPath);
        if (violations.Count > 0)
        {
            _services.Log("  REJECTED — " + violations.Count + " violation(s):");
            foreach (var v in violations)
                _services.Log("    [" + v.Severity + "] " + v.Method + ": " + v.Description);
            return;
        }
        _services.Log("  IL scan passed");

        var hostAlc = AssemblyLoadContext.GetLoadContext(typeof(PluginManager).Assembly)!;
        var alc = new PluginLoadContext(dllPath, hostAlc);
        var asm = alc.LoadFromAssemblyPath(dllPath);

        var pluginType = asm.GetTypes()
            .FirstOrDefault(t => typeof(IGamePlugin).IsAssignableFrom(t) && !t.IsAbstract);

        if (pluginType == null)
        {
            _services.Log("  ERROR: No IGamePlugin implementation found");
            alc.Unload();
            return;
        }

        var plugin = (IGamePlugin)Activator.CreateInstance(pluginType)!;
        plugin.OnLoad(_services);
        _plugins.Add(new LoadedPlugin(alc, plugin));
        _services.Log("  Loaded: " + pluginType.FullName);
    }

    public void UpdateAll(float dt)
    {
        foreach (var p in _plugins)
            p.Plugin.OnUpdate(dt);
    }

    public void UnloadAll()
    {
        foreach (var p in _plugins)
        {
            p.Plugin.OnUnload();
            p.Context.Unload();
        }
        _plugins.Clear();
        _services.Log("All plugins unloaded");
    }

    private record LoadedPlugin(AssemblyLoadContext Context, IGamePlugin Plugin);
}

internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDir;
    private readonly AssemblyLoadContext _hostAlc;

    public PluginLoadContext(string pluginPath, AssemblyLoadContext hostAlc)
        : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
        _pluginDir = Path.GetDirectoryName(pluginPath)!;
        _hostAlc = hostAlc;
    }

    protected override Assembly? Load(AssemblyName name)
    {
        // Plugin-private dependencies load from the plugin's directory.
        string candidate = Path.Combine(_pluginDir, name.Name + ".dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        // Shared assemblies (PluginApi, BCL) resolve from the engine's ALC,
        // which was created by hostfxr. This ensures type identity: the
        // IGamePlugin the plugin implements is the same type the engine sees.
        try { return _hostAlc.LoadFromAssemblyName(name); }
        catch { return null; }
    }
}
