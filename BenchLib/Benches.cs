using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BenchLib;

public interface IAnimal { int Speak(); }
public class Dog : IAnimal { [MethodImpl(MethodImplOptions.NoInlining)] public int Speak() => 1; }
public class Cat : IAnimal { [MethodImpl(MethodImplOptions.NoInlining)] public int Speak() => 2; }
public class PropBox { public int Value { get; set; } }

public static class Benches
{
    public static (double ms, int checksum) DictLookup(int n, int keyCount)
    {
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
        return (sw.Elapsed.TotalMilliseconds, sum);
    }

    public static (double ms, double sink) MathLoop(int n)
    {
        var sw = Stopwatch.StartNew();
        double x = 1.0;
        for (int i = 0; i < n; i++)
            x = (x * 1.0000001 + i * 0.0000001) % 97.0;
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, x);
    }

    public static (double ms, int sink) InterfaceDispatch(int n)
    {
        IAnimal dog = new Dog();
        IAnimal cat = new Cat();
        var sw = Stopwatch.StartNew();
        int v = 0;
        for (int i = 0; i < n; i++)
            v += ((i & 1) == 0 ? dog : cat).Speak();
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, v);
    }

    public static (double ms, int sink) PropertyAccess(int n)
    {
        var box = new PropBox();
        var sw = Stopwatch.StartNew();
        int v = 0;
        for (int i = 0; i < n; i++)
        {
            box.Value = i;
            v += box.Value;
        }
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, v);
    }

    public static (double ms, int sink) ObjectAlloc(int n)
    {
        var sw = Stopwatch.StartNew();
        int v = 0;
        for (int i = 0; i < n; i++)
        {
            var obj = new PropBox();
            obj.Value = i;
            v += obj.Value;
        }
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, v);
    }

    public static (double addMs, double iterMs, int sum) ListBench(int n)
    {
        var list = new List<int>(n);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < n; i++)
            list.Add(i);
        sw.Stop();
        double addMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        int sum = 0;
        for (int i = 0; i < list.Count; i++)
            sum += list[i];
        sw.Stop();
        return (addMs, sw.Elapsed.TotalMilliseconds, sum);
    }

    public static (double ms, int sink) TryCatch(int n)
    {
        var sw = Stopwatch.StartNew();
        int v = 0;
        for (int i = 0; i < n; i++)
        {
            try { v += i; }
            catch (Exception) { }
        }
        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds, v);
    }
}
