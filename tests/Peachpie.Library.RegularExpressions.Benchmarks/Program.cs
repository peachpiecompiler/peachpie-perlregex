using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace Peachpie.Library.RegularExpressions.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp30)]
    [MemoryDiagnoser]
    public class ReduxBenchmarkQuick
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default
                    .With(CoreRuntime.Core30));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Add(new EtwProfiler());
                }
            }
        }

        private string _sequences;

        [GlobalSetup]
        public void Setup()
        {
            _sequences = File.ReadAllText("regexredux-input10000.txt");
        }

        [Benchmark(Baseline = true)]
        public RegexRedux.Result CoreFx() => RegexRedux.RunCoreFx(_sequences);

        [Benchmark]
        public RegexRedux.Result Pcre() => RegexRedux.RunPcre(_sequences);
    }

    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}