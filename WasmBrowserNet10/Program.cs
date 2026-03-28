using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;

using System.Threading.Tasks;
using BrowserPluginApi;

namespace Sample
{
    public partial class Test
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== Host-side benchmarks (AOT compiled, TrimMode=partial) ===");
            RunHostBenchmarks();

            Console.WriteLine();
            Console.WriteLine("=== Loading plugin dynamically via HTTP... ===");
            await LoadAndRunPlugin();

            Console.WriteLine();
            Console.WriteLine("=== All done ===");
            return 0;
        }

        [JSImport("globalThis.getBaseUri")]
        internal static partial string GetBaseUri();

        [JSExport]
        public static async Task RunAll()
        {
            Console.WriteLine("=== Host-side benchmarks (AOT compiled) ===");
            RunHostBenchmarks();

            Console.WriteLine();
            Console.WriteLine("=== Loading plugin dynamically via HTTP... ===");
            await LoadAndRunPlugin();

            Console.WriteLine();
            Console.WriteLine("=== All done ===");
        }

        static async Task LoadAndRunPlugin()
        {
            try
            {
                var baseUri = GetBaseUri();
                Console.WriteLine("  Base URI: " + baseUri);
                using var http = new HttpClient { BaseAddress = new Uri(baseUri) };
                var dllBytes = await http.GetByteArrayAsync("plugins/BrowserPlugin.dll");
                Console.WriteLine("  Downloaded BrowserPlugin.dll (" + dllBytes.Length + " bytes)");

                var asm = Assembly.Load(dllBytes);
                Console.WriteLine("  Assembly loaded: " + asm.FullName);

                foreach (var type in asm.GetTypes())
                {
                    if (typeof(IBrowserPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var plugin = (IBrowserPlugin)Activator.CreateInstance(type)!;
                        Console.WriteLine("  Found plugin: " + plugin.Name);
                        Console.WriteLine();
                        plugin.Run(Console.WriteLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Plugin loading failed: " + ex.GetType().Name + ": " + ex.Message);
                Console.WriteLine("  " + ex.StackTrace);
            }
        }

        static void RunHostBenchmarks()
        {
            const int n = 1_000_000;
            const int keyCount = 10_000;

            Console.WriteLine("  [Host - AOT] running benchmarks (n=" + n.ToString() + ")");

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
            Console.WriteLine("    dict lookup        : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms (checksum " + sum.ToString() + ")");

            sw.Restart();
            double x = 1.0;
            for (int i = 0; i < n; i++)
                x = (x * 1.0000001 + i * 0.0000001) % 97.0;
            sw.Stop();
            Console.WriteLine("    math loop          : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms (sink " + x.ToString("F6") + ")");

            IAnimal dog = new Dog();
            IAnimal cat = new Cat();
            sw.Restart();
            int v = 0;
            for (int i = 0; i < n; i++)
                v += ((i & 1) == 0 ? dog : cat).Speak();
            sw.Stop();
            Console.WriteLine("    interface dispatch  : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            var box = new PropBox();
            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                box.Value = i;
                v += box.Value;
            }
            sw.Stop();
            Console.WriteLine("    property get+set   : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                var obj = new PropBox();
                obj.Value = i;
                v += obj.Value;
            }
            sw.Stop();
            Console.WriteLine("    object alloc+use   : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            var list = new List<int>(n);
            sw.Restart();
            for (int i = 0; i < n; i++)
                list.Add(i);
            sw.Stop();
            Console.WriteLine("    List<int> add      : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            sw.Restart();
            int listSum = 0;
            for (int i = 0; i < list.Count; i++)
                listSum += list[i];
            sw.Stop();
            Console.WriteLine("    List<int> iterate  : " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            sw.Restart();
            for (int i = 0; i < n; i++)
            {
                try { v += i; }
                catch (Exception) { }
            }
            sw.Stop();
            Console.WriteLine("    try/catch (no throw): " + sw.Elapsed.TotalMilliseconds.ToString("F2") + " ms");

            Console.WriteLine("    (sink " + (v + listSum).ToString() + ")");
        }
    }

    internal interface IAnimal { int Speak(); }
    internal class Dog : IAnimal { public int Speak() => 1; }
    internal class Cat : IAnimal { public int Speak() => 2; }
    internal class PropBox { public int Value { get; set; } }
}
