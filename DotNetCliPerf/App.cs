﻿using BenchmarkDotNet.Attributes;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DotNetCliPerf
{
    public abstract class App: RootTemp
    {
        protected static readonly TimeSpan SleepBetweenHttpRequests = TimeSpan.FromMilliseconds(100);

        protected static readonly HttpClient HttpClient = new HttpClient();

        protected readonly Dictionary<string, string> Environment = new Dictionary<string, string>();

        protected string OldValue { get; set; } = "InitialValue";
        protected string NewValue { get; set; }

        private static readonly string _defaultOutput = Guid.NewGuid().ToString();
        private string Output { get; set; } = _defaultOutput;

        protected abstract string SourceDir { get; }
        protected abstract string SourcePath { get; }
        protected abstract string ExpectedOutput { get; }

        protected abstract void Build(bool first = false);
        protected abstract string Run(bool first = false);

        protected virtual void RunCleanup() { }

        [Params(SourceChanged.Leaf, SourceChanged.Root)]
        public SourceChanged SourceChanged { get; set; }

        public override void GlobalSetup()
        {
            base.GlobalSetup();
            CopyApp();
            ChangeSource();
            Output = Run(first: true);
            VerifyOutput();
        }

        protected virtual void CopyApp()
        {
            Util.DirectoryCopy(Path.Combine(Util.RepoRoot, "scenarios", SourceDir), RootTempDir, copySubDirs: true);
        }

        private void Clean()
        {
            base.GlobalCleanup();
            CopyApp();
            ChangeSource();
        }

        protected void ChangeSource()
        {
            NewValue = Guid.NewGuid().ToString();
            Util.ReplaceInFile(Path.Combine(RootTempDir, SourcePath), $"\"{OldValue}\"", $"\"{NewValue}\"");
        }

        private void VerifyOutput()
        {
            if (Output != _defaultOutput && !Output.Contains(ExpectedOutput))
            {
                throw new InvalidOperationException($"Response missing '{ExpectedOutput}'");
            }

            Output = _defaultOutput;
            OldValue = NewValue;

            RunCleanup();
        }

        [IterationSetup(Target = nameof(BuildIncrementalSourceChanged))]
        public void IterationSetupBuildIncrementalSourceChanged()
        {
            ChangeSource();
        }

        [Benchmark]
        public void BuildIncrementalSourceChanged()
        {
            Build();
        }

        [Benchmark]
        public void BuildIncrementalNoChange()
        {
            Build();
        }

        [IterationSetup(Target = nameof(BuildFull))]
        public void IterationSetupBuildFull()
        {
            Clean();
        }

        [Benchmark]
        public void BuildFull()
        {
            Build(first: true);
        }

        [IterationSetup(Target = nameof(RunIncrementalSourceChanged))]
        public void IterationSetupRunIncrementalSourceChanged()
        {
            ChangeSource();
        }

        [Benchmark]
        public void RunIncrementalSourceChanged()
        {
            Output = Run();
        }

        [IterationCleanup(Target = nameof(RunIncrementalSourceChanged))]
        public void IterationCleanupRunIncrementalSourceChanged()
        {
            VerifyOutput();
        }

        [Benchmark]
        public void RunIncrementalNoChange()
        {
            Output = Run();
        }

        [IterationCleanup(Target = nameof(RunIncrementalNoChange))]
        public void IterationCleanupRunIncrementalNoChange()
        {
            VerifyOutput();
        }

        [IterationSetup(Target = nameof(RunFull))]
        public void IterationSetupRunFull()
        {
            Clean();
        }

        [Benchmark]
        public void RunFull()
        {
            Output = Run(first: true);
        }

        [IterationCleanup(Target = nameof(RunFull))]
        public void IterationCleanupRunFull()
        {
            VerifyOutput();
        }
    }
}
