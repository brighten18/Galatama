using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GALATAMA.MainMenu;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.Benchmarking
{
    public class BenchmarkMetricsLogger : MonoBehaviour
    {
        [Header("Run Info")]
        [SerializeField] private string scenarioName = "Route A";

        [Header("LOD Verification")]
        [SerializeField] private LodCaptureMode lodCaptureMode = LodCaptureMode.UseSavedSetting;
        [SerializeField] private bool reapplyLodAtRunStart = true;

        [Header("Capture Timing")]
        [SerializeField] private bool startOnPlay = true;
        [SerializeField] private float warmupSeconds = 10f;
        [SerializeField] private float captureSeconds = 60f;
        [SerializeField] private float sampleIntervalSeconds = 1f;

        [Header("Output")]
        [SerializeField] private string outputFolderName = "BenchmarkResults";
        [SerializeField] private bool writePerFrameCsv = true;
        [SerializeField] private bool appendToSummaryFile = true;

        [Header("Optional Hooks")]
        [SerializeField] private BenchmarkAutoPilot autoPilot;

        private const int RecorderCapacity = 4;

        private readonly List<SecondSample> frameSamples = new List<SecondSample>(4096);
        private readonly StringBuilder logBuilder = new StringBuilder(256);

        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder memoryRecorder;
        private bool drawCallsAvailable;
        private bool memoryAvailable;

        private bool runActive;
        private bool captureActive;
        private float elapsedSinceRunStart;
        private float elapsedCaptureTime;
        private float elapsedSinceLastSample;
        private string outputFolderPath;
        private bool expectedLodEnabled;
        private int lastAppliedLodGroupCount;
        private DateTime runStartedAt;

        private struct SecondSample
        {
            public int secondIndex;
            public double fps;
            public double cpuMs;
            public double gpuMs;
            public long drawCalls;
            public double memoryMb;
            public int matchingLodGroups;
            public int totalLodGroups;
            public bool lodVerified;
        }

        private enum LodCaptureMode
        {
            UseSavedSetting = 0,
            ForceOn = 1,
            ForceOff = 2
        }

        private void OnEnable()
        {
            InitializeRecorders();

            if (startOnPlay)
            {
                BeginRun();
            }
        }

        private void OnDisable()
        {
            EndRun(saveResults: false);
            DisposeRecorders();
        }

        private void Update()
        {
            if (!runActive)
            {
                return;
            }

            elapsedSinceRunStart += Time.unscaledDeltaTime;

            if (!captureActive)
            {
                if (elapsedSinceRunStart >= warmupSeconds)
                {
                    captureActive = true;
                    elapsedCaptureTime = 0f;
                    elapsedSinceLastSample = 0f;
                    frameSamples.Clear();
                    Debug.Log("[BenchmarkMetricsLogger] Warm-up selesai. Capture dimulai.");
                }
                else
                {
                    return;
                }
            }

            elapsedCaptureTime += Time.unscaledDeltaTime;
            elapsedSinceLastSample += Time.unscaledDeltaTime;

            float sampleInterval = Mathf.Max(0.1f, sampleIntervalSeconds);
            if (elapsedSinceLastSample >= sampleInterval)
            {
                CollectSample();
                elapsedSinceLastSample -= sampleInterval;
            }

            if (elapsedCaptureTime >= captureSeconds)
            {
                EndRun(saveResults: true);
            }
        }

        [ContextMenu("Begin Benchmark Run")]
        public void BeginRun()
        {
            outputFolderPath = Path.Combine(Application.persistentDataPath, outputFolderName);
            Directory.CreateDirectory(outputFolderPath);
            runStartedAt = DateTime.Now;

            expectedLodEnabled = ResolveExpectedLodEnabled();
            if (reapplyLodAtRunStart)
            {
                lastAppliedLodGroupCount = LodSettingsUtility.ApplyLodModeToAllGroups(expectedLodEnabled);
            }
            else
            {
                lastAppliedLodGroupCount = 0;
            }

            if (autoPilot != null)
            {
                autoPilot.SetRunning(true);
            }

            frameSamples.Clear();
            elapsedSinceRunStart = 0f;
            elapsedCaptureTime = 0f;
            elapsedSinceLastSample = 0f;
            captureActive = false;
            runActive = true;

            Debug.Log("[BenchmarkMetricsLogger] Run dimulai. Hasil akan disimpan ke: " + outputFolderPath);
        }

        [ContextMenu("Stop And Save Benchmark Run")]
        public void StopAndSaveBenchmarkRun()
        {
            if (!runActive)
            {
                return;
            }

            EndRun(saveResults: true);
        }

        private void EndRun(bool saveResults)
        {
            if (!runActive)
            {
                return;
            }

            runActive = false;
            captureActive = false;

            if (autoPilot != null)
            {
                autoPilot.SetRunning(false);
            }

            if (saveResults && frameSamples.Count > 0)
            {
                SaveResults();
            }
        }

        private void CollectSample()
        {
            FrameTimingManager.CaptureFrameTimings();

            FrameTiming[] timings = new FrameTiming[1];
            uint timingCount = FrameTimingManager.GetLatestTimings(1, timings);

            double cpuMs = timingCount > 0 ? timings[0].cpuFrameTime : Time.unscaledDeltaTime * 1000.0;
            double gpuMs = timingCount > 0 ? timings[0].gpuFrameTime : 0.0;
            double fps = cpuMs > 0.0001 ? 1000.0 / cpuMs : 0.0;

            long drawCalls = drawCallsAvailable ? drawCallsRecorder.LastValue : -1;
            double memoryMb = memoryAvailable ? memoryRecorder.LastValue / (1024.0 * 1024.0) : -1.0;
            LodSettingsUtility.LodVerificationResult lodVerification = LodSettingsUtility.VerifyLodMode(expectedLodEnabled);

            frameSamples.Add(new SecondSample
            {
                secondIndex = frameSamples.Count + 1,
                fps = fps,
                cpuMs = cpuMs,
                gpuMs = gpuMs,
                drawCalls = drawCalls,
                memoryMb = memoryMb,
                matchingLodGroups = lodVerification.matchingGroups,
                totalLodGroups = lodVerification.totalGroups,
                lodVerified = lodVerification.isFullyApplied
            });
        }

        private void SaveResults()
        {
            BenchmarkSummary summary = BuildSummary();
            string timestamp = runStartedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string safeScenario = MakeSafeFileToken(scenarioName);
            string safeLod = MakeSafeFileToken(GetExpectedLodLabel());

            string summaryFilePath = Path.Combine(outputFolderPath, "benchmark_summary.csv");
            WriteSummaryCsv(summaryFilePath, summary, appendToSummaryFile);

            if (writePerFrameCsv)
            {
                string frameFileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}_{2}_frames.csv",
                    safeScenario,
                    safeLod,
                    timestamp);

                WritePerFrameCsv(Path.Combine(outputFolderPath, frameFileName));
            }

            logBuilder.Length = 0;
            logBuilder.AppendLine("[BenchmarkMetricsLogger] Benchmark selesai.");
            logBuilder.Append("Summary CSV: ").Append(summaryFilePath).AppendLine();
            logBuilder.Append("Expected LOD: ").Append(summary.expectedLodLabel)
                .Append(" | Verified: ").Append(summary.lodVerificationPassed ? "YES" : "NO").AppendLine();
            logBuilder.Append("Avg FPS: ").Append(summary.avgFps.ToString("F2", CultureInfo.InvariantCulture)).AppendLine();
            logBuilder.Append("Min FPS: ").Append(summary.minFps.ToString("F2", CultureInfo.InvariantCulture)).AppendLine();
            logBuilder.Append("Avg Draw Calls: ").Append(summary.avgDrawCalls.ToString("F0", CultureInfo.InvariantCulture)).AppendLine();
            logBuilder.Append("Avg GPU Time (ms): ").Append(summary.avgGpuMs.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
            logBuilder.Append("Avg CPU Time (ms): ").Append(summary.avgCpuMs.ToString("F3", CultureInfo.InvariantCulture)).AppendLine();
            logBuilder.Append("Avg Memory (MB): ").Append(summary.avgMemoryMb.ToString("F2", CultureInfo.InvariantCulture));
            Debug.Log(logBuilder.ToString());
        }

        private BenchmarkSummary BuildSummary()
        {
            double fpsSum = 0.0;
            double minFps = double.MaxValue;
            double cpuSum = 0.0;
            double gpuSum = 0.0;
            double drawCallSum = 0.0;
            double memorySum = 0.0;
            int gpuCount = 0;
            int drawCallCount = 0;
            int memoryCount = 0;
            int verifiedSampleCount = 0;
            int latestMatchingLodGroups = 0;
            int latestTotalLodGroups = 0;

            for (int i = 0; i < frameSamples.Count; i++)
            {
                SecondSample sample = frameSamples[i];
                fpsSum += sample.fps;
                if (sample.fps < minFps)
                {
                    minFps = sample.fps;
                }

                cpuSum += sample.cpuMs;

                if (sample.gpuMs > 0.0)
                {
                    gpuSum += sample.gpuMs;
                    gpuCount++;
                }

                if (sample.drawCalls >= 0)
                {
                    drawCallSum += sample.drawCalls;
                    drawCallCount++;
                }

                if (sample.memoryMb >= 0.0)
                {
                    memorySum += sample.memoryMb;
                    memoryCount++;
                }

                if (sample.lodVerified)
                {
                    verifiedSampleCount++;
                }

                latestMatchingLodGroups = sample.matchingLodGroups;
                latestTotalLodGroups = sample.totalLodGroups;
            }

            int totalFrames = frameSamples.Count;
            return new BenchmarkSummary
            {
                capturedAt = DateTime.Now,
                runStartedAt = runStartedAt,
                sceneName = SceneManager.GetActiveScene().name,
                scenarioName = scenarioName,
                expectedLodLabel = GetExpectedLodLabel(),
                sampleCount = totalFrames,
                warmupSeconds = warmupSeconds,
                captureSeconds = elapsedCaptureTime,
                avgFps = totalFrames > 0 ? fpsSum / totalFrames : 0.0,
                minFps = totalFrames > 0 ? minFps : 0.0,
                avgCpuMs = totalFrames > 0 ? cpuSum / totalFrames : 0.0,
                avgGpuMs = gpuCount > 0 ? gpuSum / gpuCount : -1.0,
                avgDrawCalls = drawCallCount > 0 ? drawCallSum / drawCallCount : -1.0,
                avgMemoryMb = memoryCount > 0 ? memorySum / memoryCount : -1.0,
                totalLodGroups = latestTotalLodGroups,
                matchingLodGroups = latestMatchingLodGroups,
                lodVerificationPassed = totalFrames > 0 && verifiedSampleCount == totalFrames,
                reapplyLodGroupCount = lastAppliedLodGroupCount
            };
        }

        private void WriteSummaryCsv(string filePath, BenchmarkSummary summary, bool append)
        {
            bool shouldWriteHeader = !append || !File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, append, Encoding.UTF8))
            {
                if (shouldWriteHeader)
                {
                    writer.WriteLine("Timestamp,RunStartedAt,Scene,Scenario,ExpectedLOD,SampleCount,WarmupSeconds,CaptureSeconds,AvgFPS,MinFPS,AvgDrawCalls,AvgGPUTimeMs,AvgCPUTimeMs,AvgMemoryMB,TotalLODGroups,MatchingLODGroups,LODVerified,ReappliedLODGroups");
                }

                writer.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6:F2},{7:F2},{8:F3},{9:F3},{10:F3},{11:F3},{12:F3},{13:F3},{14},{15},{16},{17}",
                    summary.capturedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    summary.runStartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    EscapeCsv(summary.sceneName),
                    EscapeCsv(summary.scenarioName),
                    EscapeCsv(summary.expectedLodLabel),
                    summary.sampleCount,
                    summary.warmupSeconds,
                    summary.captureSeconds,
                    summary.avgFps,
                    summary.minFps,
                    summary.avgDrawCalls,
                    summary.avgGpuMs,
                    summary.avgCpuMs,
                    summary.avgMemoryMb,
                    summary.totalLodGroups,
                    summary.matchingLodGroups,
                    summary.lodVerificationPassed ? "TRUE" : "FALSE",
                    summary.reapplyLodGroupCount));
            }
        }

        private void WritePerFrameCsv(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("SecondIndex,FPS,CPUTimeMs,GPUTimeMs,DrawCalls,MemoryMB,ExpectedLOD,MatchingLODGroups,TotalLODGroups,LODVerified");

                for (int i = 0; i < frameSamples.Count; i++)
                {
                    SecondSample sample = frameSamples[i];
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0},{1:F4},{2:F4},{3:F4},{4},{5:F4},{6},{7},{8},{9}",
                        sample.secondIndex,
                        sample.fps,
                        sample.cpuMs,
                        sample.gpuMs,
                        sample.drawCalls,
                        sample.memoryMb,
                        GetExpectedLodLabel(),
                        sample.matchingLodGroups,
                        sample.totalLodGroups,
                        sample.lodVerified ? "TRUE" : "FALSE"));
                }
            }
        }

        private bool ResolveExpectedLodEnabled()
        {
            switch (lodCaptureMode)
            {
                case LodCaptureMode.ForceOn:
                    return true;
                case LodCaptureMode.ForceOff:
                    return false;
                default:
                    return LodSettingsUtility.GetSavedLodEnabled();
            }
        }

        private string GetExpectedLodLabel()
        {
            return expectedLodEnabled ? "ON" : "OFF";
        }

        private void InitializeRecorders()
        {
            drawCallsRecorder = CreateRecorder(
                ProfilerCategory.Render,
                new[] { "Draw Calls Count", "Draw Calls" });
            drawCallsAvailable = drawCallsRecorder.Valid;

            memoryRecorder = CreateRecorder(
                ProfilerCategory.Memory,
                new[] { "System Used Memory", "Total Used Memory" });
            memoryAvailable = memoryRecorder.Valid;
        }

        private static ProfilerRecorder CreateRecorder(ProfilerCategory category, string[] statNames)
        {
            for (int i = 0; i < statNames.Length; i++)
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, statNames[i], RecorderCapacity);
                if (recorder.Valid)
                {
                    return recorder;
                }

                recorder.Dispose();
            }

            return default;
        }

        private void DisposeRecorders()
        {
            if (drawCallsRecorder.Valid)
            {
                drawCallsRecorder.Dispose();
            }

            if (memoryRecorder.Valid)
            {
                memoryRecorder.Dispose();
            }
        }

        private static string MakeSafeFileToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "benchmark";
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        [Serializable]
        private struct BenchmarkSummary
        {
            public DateTime capturedAt;
            public DateTime runStartedAt;
            public string sceneName;
            public string scenarioName;
            public string expectedLodLabel;
            public int sampleCount;
            public float warmupSeconds;
            public float captureSeconds;
            public double avgFps;
            public double minFps;
            public double avgDrawCalls;
            public double avgGpuMs;
            public double avgCpuMs;
            public double avgMemoryMb;
            public int totalLodGroups;
            public int matchingLodGroups;
            public bool lodVerificationPassed;
            public int reapplyLodGroupCount;
        }
    }
}
