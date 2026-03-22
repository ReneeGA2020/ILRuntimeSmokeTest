using PluginApi;

namespace SamplePlugin;

public class MyPlugin : IGamePlugin
{
    private IEngineServices? _engine;
    private int _tickCount;

    public void OnLoad(IEngineServices engine)
    {
        _engine = engine;
        _engine.Log("SamplePlugin loaded!");
    }

    public void OnUpdate(float deltaTime)
    {
        _tickCount++;
        int frame = _engine!.GetFrame();
        _engine.Log("  SamplePlugin.Update: tick=" + _tickCount + " frame=" + frame);
    }

    public void OnUnload()
    {
        _engine?.Log("SamplePlugin unloading (ran " + _tickCount + " ticks)");
    }
}
