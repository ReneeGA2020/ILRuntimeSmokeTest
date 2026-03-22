using IlrtAppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

string baseDir = AppContext.BaseDirectory;
string dll = Path.Combine(baseDir, "hot", "IlrtHotDll.dll");

if (!File.Exists(dll))
{
    Console.Error.WriteLine($"未找到 {dll}。请先构建解决方案以便复制热更 DLL。");
    return 1;
}

byte[] dllBytes = File.ReadAllBytes(dll);
using var dllStream = new MemoryStream(dllBytes, writable: false);

var app = new IlrtAppDomain();
app.LoadAssembly(dllStream);

Console.WriteLine(app.Invoke("IlrtHotDll.HotEntry", "Run", null, "ILRuntime"));
Console.WriteLine(app.Invoke("IlrtHotDll.HotEntry", "RunBenchmarks", null));
Console.WriteLine(app.Invoke("IlrtHotDll.HotEntry", "RunDetailedBenchmarks", null));
Console.WriteLine(app.Invoke("IlrtHotDll.HotEntry", "RunCrossCallBenchmark", null));
Console.WriteLine(app.Invoke("IlrtHotDll.HotEntry", "RunBenchmarksHostAssist", null));

app.Dispose();
return 0;

