using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;

namespace Peachpie.Library.RegularExpressions.Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp30)]
    [MemoryDiagnoser]
    public class ReduxBenchmarkQuick
    {
        private string _sequences;

        [GlobalSetup]
        public void Setup()
        {
            _sequences = File.ReadAllText("regexredux-input10000.txt");
        }

        [Benchmark(Baseline = true)]
        public RegexRedux.Result CoreFx() => RegexRedux.RunCoreFx(_sequences);

        [Benchmark]
        public RegexRedux.Result Peachpie() => RegexRedux.RunPeachpie(_sequences);
    }

    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class ReduxBenchmarkSlow
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default
                    .With(CoreRuntime.Core30)
                    .WithLaunchCount(1)
                    .WithWarmupCount(1)
                    .WithInvocationCount(1)
                    .WithUnrollFactor(1)
                    .WithIterationCount(3));
            }
        }

        private string _sequences;

        [GlobalSetup]
        public void Setup()
        {
            _sequences = File.ReadAllText("regexredux-input5000000.txt");
        }

        [Benchmark(Baseline = true)]
        public RegexRedux.Result CoreFx() => RegexRedux.RunCoreFx(_sequences);

        [Benchmark]
        public RegexRedux.Result Peachpie() => RegexRedux.RunPeachpie(_sequences);
    }

    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}