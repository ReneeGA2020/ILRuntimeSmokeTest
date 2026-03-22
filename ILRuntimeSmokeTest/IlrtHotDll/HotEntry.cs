using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IlrtHotDll
{
    public interface IAnimal { int Speak(); }
    public class Dog : IAnimal { public int Speak() => 1; }
    public class Cat : IAnimal { public int Speak() => 2; }

    public class PropBox { public int Value { get; set; } }

    public static class HotEntry
    {
        public static string Run(string name) => "[IlrtHotDll] hello, " + name;

        public static string RunBenchmarks()
        {
            const int n = 1_000_000;
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
            double dictMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            double x = 1.0;
            for (int i = 0; i < n; i++)
                x = (x * 1.0000001 + i * 0.0000001) % 97.0;
            sw.Stop();
            double mathMs = sw.Elapsed.TotalMilliseconds;

            return "[IlrtHotDll] benchmarks (n=" + n.ToString() + ", interpreted)\n" +
                "  dict lookup : " + dictMs.ToString("F2") + " ms (checksum " + sum.ToString() + ")\n" +
                "  math loop   : " + mathMs.ToString("F2") + " ms (sink " + x.ToString("F6") + ")";
        }

        public static string RunCrossCallBenchmark()
        {
            const int n = 1_000_000;
            var sw = Stopwatch.StartNew();
            int v = 0;
            for (int i = 0; i < n; i++)
                v = IlrtShared.HostHelpers.Nop(v);
            sw.Stop();
            double ms = sw.Elapsed.TotalMilliseconds;
            double ns = ms * 1_000_000.0 / n;
            return "[IlrtHotDll] cross-call (n=" + n.ToString() + ")\n" +
                "  total    : " + ms.ToString("F2") + " ms\n" +
                "  per call : " + ns.ToString("F0") + " ns (sink " + v.ToString() + ")";
        }

        public static string RunBenchmarksHostAssist()
        {
            const int n = 1_000_000;
            const int keyCount = 10_000;

            var keys = new string[keyCount];
            for (int i = 0; i < keyCount; i++)
                keys[i] = "k" + i.ToString();

            var dict = new Dictionary<string, int>(keyCount, StringComparer.Ordinal);
            for (int i = 0; i < keyCount; i++)
                dict[keys[i]] = i;

            var sw = Stopwatch.StartNew();
            int sum = IlrtShared.HostHelpers.SumLookups(dict, keys, n);
            sw.Stop();
            double dictMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            double x = IlrtShared.HostHelpers.MathLoop(n);
            sw.Stop();
            double mathMs = sw.Elapsed.TotalMilliseconds;

            return "[IlrtHotDll] benchmarks (n=" + n.ToString() + ", host-assisted)\n" +
                "  dict lookup : " + dictMs.ToString("F2") + " ms (checksum " + sum.ToString() + ")\n" +
                "  math loop   : " + mathMs.ToString("F2") + " ms (sink " + x.ToString("F6") + ")";
        }

        public static string RunDetailedBenchmarks()
        {
            const int n = 1_000_000;

            // 1. 虚方法 / 接口调用
            IAnimal dog = new Dog();
            IAnimal cat = new Cat();
            var sw = Stopwatch.StartNew();
            int v = 0;
            for (int i = 0; i < n; i++)
                v += ((i & 1) == 0 ? dog : cat).Speak();
            sw.Stop();
            double interfaceMs = sw.Elapsed.TotalMilliseconds;

            // 2. 属性读写（get/set 各一次）
            var box = new PropBox();
            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                box.Value = i;
                v += box.Value;
            }
            sw.Stop();
            double propMs = sw.Elapsed.TotalMilliseconds;

            // 3. 对象分配
            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                var obj = new PropBox();
                obj.Value = i;
                v += obj.Value;
            }
            sw.Stop();
            double allocMs = sw.Elapsed.TotalMilliseconds;

            // 4. List<int> 填充 + 遍历
            var list = new List<int>(n);
            sw.Restart();
            for (int i = 0; i < n; i++)
                list.Add(i);
            sw.Stop();
            double listAddMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            int listSum = 0;
            for (int i = 0; i < list.Count; i++)
                listSum += list[i];
            sw.Stop();
            double listIterMs = sw.Elapsed.TotalMilliseconds;

            // 5. try/catch 循环（无实际异常抛出）
            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                try { v += i; }
                catch (Exception) { }
            }
            sw.Stop();
            double tryMs = sw.Elapsed.TotalMilliseconds;

            return "[IlrtHotDll] detailed benchmarks (n=" + n.ToString() + ")\n" +
                "  interface dispatch  : " + interfaceMs.ToString("F2") + " ms\n" +
                "  property get+set   : " + propMs.ToString("F2") + " ms\n" +
                "  object alloc+use   : " + allocMs.ToString("F2") + " ms\n" +
                "  List<int> add      : " + listAddMs.ToString("F2") + " ms\n" +
                "  List<int> iterate  : " + listIterMs.ToString("F2") + " ms\n" +
                "  try/catch (no throw) : " + tryMs.ToString("F2") + " ms\n" +
                "  (sink " + (v + listSum).ToString() + ")";
        }
    }
}
