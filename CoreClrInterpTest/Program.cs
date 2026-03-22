using System.Diagnostics;
using System.Runtime.CompilerServices;

const int N = 1_000_000;
const int KeyCount = 10_000;

Console.WriteLine($"[CoreCLR Interp Test] PID={Environment.ProcessId}");
Console.WriteLine($"  Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"  InterpMode: {Environment.GetEnvironmentVariable("DOTNET_InterpMode") ?? "(not set)"}");
Console.WriteLine();

Console.WriteLine("--- Local (interpreted under Mode 1) ---");
RunDictBenchmark();
RunMathBenchmark();
RunInterfaceDispatch();
RunPropertyAccess();
RunObjectAlloc();
RunListBenchmark();
RunTryCatch();

Console.WriteLine();
Console.WriteLine("--- BenchLib (R2R native under Mode 1) ---");
RunBenchLibTests();

static void RunBenchLibTests()
{
    var (dictMs, dictSum) = BenchLib.Benches.DictLookup(N, KeyCount);
    Console.WriteLine($"  dict lookup        : {dictMs,10:F2} ms  (checksum {dictSum})");

    var (mathMs, mathSink) = BenchLib.Benches.MathLoop(N);
    Console.WriteLine($"  math loop          : {mathMs,10:F2} ms  (sink {mathSink:F6})");

    var (ifaceMs, ifaceSink) = BenchLib.Benches.InterfaceDispatch(N);
    Console.WriteLine($"  interface dispatch  : {ifaceMs,10:F2} ms  (sink {ifaceSink})");

    var (propMs, propSink) = BenchLib.Benches.PropertyAccess(N);
    Console.WriteLine($"  property get+set   : {propMs,10:F2} ms  (sink {propSink})");

    var (allocMs, allocSink) = BenchLib.Benches.ObjectAlloc(N);
    Console.WriteLine($"  object alloc+use   : {allocMs,10:F2} ms  (sink {allocSink})");

    var (listAddMs, listIterMs, listSum) = BenchLib.Benches.ListBench(N);
    Console.WriteLine($"  List<int> add      : {listAddMs,10:F2} ms");
    Console.WriteLine($"  List<int> iterate  : {listIterMs,10:F2} ms  (sum {listSum})");

    var (tryMs, trySink) = BenchLib.Benches.TryCatch(N);
    Console.WriteLine($"  try/catch (no throw): {tryMs,10:F2} ms  (sink {trySink})");
}

static void RunDictBenchmark()
{
    var keys = new string[KeyCount];
    for (int i = 0; i < KeyCount; i++)
        keys[i] = "k" + i.ToString();

    var dict = new Dictionary<string, int>(KeyCount, StringComparer.Ordinal);
    for (int i = 0; i < KeyCount; i++)
        dict[keys[i]] = i;

    var sw = Stopwatch.StartNew();
    int sum = 0;
    for (int i = 0; i < N; i++)
        sum += dict[keys[i % KeyCount]];
    sw.Stop();

    Console.WriteLine($"  dict lookup        : {sw.Elapsed.TotalMilliseconds,10:F2} ms  (checksum {sum})");
}

static void RunMathBenchmark()
{
    var sw = Stopwatch.StartNew();
    double x = 1.0;
    for (int i = 0; i < N; i++)
        x = (x * 1.0000001 + i * 0.0000001) % 97.0;
    sw.Stop();

    Console.WriteLine($"  math loop          : {sw.Elapsed.TotalMilliseconds,10:F2} ms  (sink {x:F6})");
}

static void RunInterfaceDispatch()
{
    IAnimal dog = new Dog();
    IAnimal cat = new Cat();
    var sw = Stopwatch.StartNew();
    int v = 0;
    for (int i = 0; i < N; i++)
        v += ((i & 1) == 0 ? dog : cat).Speak();
    sw.Stop();

    Console.WriteLine($"  interface dispatch  : {sw.Elapsed.TotalMilliseconds,10:F2} ms  (sink {v})");
}

static void RunPropertyAccess()
{
    var box = new PropBox();
    var sw = Stopwatch.StartNew();
    int v = 0;
    for (int i = 0; i < N; i++)
    {
        box.Value = i;
        v += box.Value;
    }
    sw.Stop();

    Console.WriteLine($"  property get+set   : {sw.Elapsed.TotalMilliseconds,10:F2} ms  (sink {v})");
}

static void RunObjectAlloc()
{
    var sw = Stopwatch.StartNew();
    int v = 0;
    for (int i = 0; i < N; i++)
    {
        var obj = new PropBox();
        obj.Value = i;
        v += obj.Value;
    }
    sw.Stop();

    Console.WriteLine($"  object alloc+use   : {sw.Elapsed.TotalMilliseconds,10:F2} ms  (sink {v})");
}

static void RunListBenchmark()
{
    var list = new List<int>(N);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < N; i++)
        list.Add(i);
    sw.Stop();
    double addMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    int sum = 0;
    for (int i = 0; i < list.Count; i++)
        sum += list[i];
    sw.Stop();
    double iterMs = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"  List<int> add      : {addMs,10:F2} ms");
    Console.WriteLine($"  List<int> iterate  : {iterMs,10:F2} ms  (sum {sum})");
}

static void RunTryCatch()
{
    var sw = Stopwatch.StartNew();
    int v = 0;
    for (int i = 0; i < N; i++)
    {
        try { v += i; }
        catch (Exception) { }
    }
    sw.Stop();

    Console.WriteLine($"  try/catch (no throw): {sw.Elapsed.TotalMilliseconds,10:F2} ms  (sink {v})");
}

interface IAnimal { int Speak(); }
class Dog : IAnimal { [MethodImpl(MethodImplOptions.NoInlining)] public int Speak() => 1; }
class Cat : IAnimal { [MethodImpl(MethodImplOptions.NoInlining)] public int Speak() => 2; }
class PropBox { public int Value { get; set; } }
