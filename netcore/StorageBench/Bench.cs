using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace SimCluster {
    public static class Bench {

        public static void Auto(Action<int> bench) {
            var watch = Stopwatch.StartNew();

            var n = 1;
            var step = Stopwatch.StartNew();
            
            
            while (watch.ElapsedMilliseconds < 4000) {
                n *= 2;
                
                step.Restart();
                bench(n);
                step.Stop();
            }

            var freq = n / step.Elapsed.TotalSeconds;

            var op = TimeSpan.FromTicks(step.Elapsed.Ticks / n).TotalMilliseconds * 1000000;
            
            Console.WriteLine($"{bench.Method.DeclaringType.Name+"/"+bench.Method.Name,42}: {freq,10:F0} op/sec {op,10:F0} ns/op");
        }
    }
}