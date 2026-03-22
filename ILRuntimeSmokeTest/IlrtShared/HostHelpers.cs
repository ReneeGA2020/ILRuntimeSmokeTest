using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IlrtShared
{
    public static class HostHelpers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Nop(int x) => x;
        public static int SumLookups(Dictionary<string, int> dict, string[] keys, int n)
        {
            int sum = 0;
            for (int i = 0; i < n; i++)
                sum += dict[keys[i % keys.Length]];
            return sum;
        }

        public static double MathLoop(int n)
        {
            double x = 1.0;
            for (int i = 0; i < n; i++)
                x = (x * 1.0000001 + i * 0.0000001) % 97.0;
            return x;
        }
    }
}
