using System;

namespace BrowserPluginApi;

public interface IBrowserPlugin
{
    string Name { get; }
    void Run(Action<string> log);
}
