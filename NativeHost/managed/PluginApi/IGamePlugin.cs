namespace PluginApi;

public interface IGamePlugin
{
    void OnLoad(IEngineServices engine);
    void OnUpdate(float deltaTime);
    void OnUnload();
}

public interface IEngineServices
{
    void Log(string message);
    int GetFrame();
}
