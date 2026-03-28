using System;
using System.Collections.Generic;
using System.Diagnostics;
using BrowserPluginApi;

namespace BrowserPlugin;

public class BenchPlugin : IBrowserPlugin
{
    public string Name => "BenchPlugin (dynamically loaded, interpreted)";

    public void Run(Action<string> log)
    {
        const int n = 1_000_000;
        const int keyCount = 10_000;

        log("  [Plugin - interpreted] running benchmarks (n=" + n.ToString() + ")");

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
        log("    dict lookup        : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms (checksum " + sum.ToString() + ")");

        sw.Restart();
        double x = 1.0;
        for (int i = 0; i < n; i++)
            x = (x * 1.0000001 + i * 0.0000001) % 97.0;
        sw.Stop();
        log("    math loop          : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms (sink " + x.ToString("F6") + ")");

        IAnimal dog = new Dog();
        IAnimal cat = new Cat();
        sw.Restart();
        int v = 0;
        for (int i = 0; i < n; i++)
            v += ((i & 1) == 0 ? dog : cat).Speak();
        sw.Stop();
        log("    interface dispatch  : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        var box = new PropBox();
        sw.Restart();
        for (int i = 0; i < n; i++)
        {
            box.Value = i;
            v += box.Value;
        }
        sw.Stop();
        log("    property get+set   : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        sw.Restart();
        for (int i = 0; i < n; i++)
        {
            var obj = new PropBox();
            obj.Value = i;
            v += obj.Value;
        }
        sw.Stop();
        log("    object alloc+use   : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        var list = new List<int>(n);
        sw.Restart();
        for (int i = 0; i < n; i++)
            list.Add(i);
        sw.Stop();
        log("    List<int> add      : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        sw.Restart();
        int listSum = 0;
        for (int i = 0; i < list.Count; i++)
            listSum += list[i];
        sw.Stop();
        log("    List<int> iterate  : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        sw.Restart();
        for (int i = 0; i < n; i++)
        {
            try { v += i; }
            catch (Exception) { }
        }
        sw.Stop();
        log("    try/catch (no throw): " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

        log("    (sink " + (v + listSum).ToString() + ")");
    }
}

internal interface IAnimal { int Speak(); }
internal class Dog : IAnimal { public int Speak() => 1; }
internal class Cat : IAnimal { public int Speak() => 2; }
internal class PropBox { public int Value { get; set; } }
