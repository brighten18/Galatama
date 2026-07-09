using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.Benchmarking
{
    public class MiniFpsBenchmarkLogger : MonoBehaviour
    {
        [Header("Run Info")]
        [SerializeField] private string scenarioName = "Mini Benchmark";

        [Header("Timing")]
        [SerializeField] private bool startOnPlay = true;
        [SerializeField] private float warmupSeconds = 3f;
        [SerializeField] private float captureSeconds = 20f;
        [SerializeField] private float sampleIntervalSeconds = 1f;

        [Header("Output")]
        [SerializeField] private string outputFolderName = "BenchmarkResults";
        [SerializeField] private string outputFilePrefix = "mini_fps";

        private readonly List<FpsSample> samples = new List<FpsSample>(256);

        private bool isRunning;
        private bool isCapturing;
        private float runTimer;
        private float captureTimer;
        private float sampleTimer;
        private float frameTimer;
        private int frameCount;
        private string outputFolderPath;
        private DateTime startedAt;

        private struct FpsSample
        {
            public int secondIndex;
            public float durationSeconds;
            public int frameCount;
            public float avgFps;
        }

        private void OnEnable()
        {
            if (startOnPlay)
            {
                BeginRun();
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            runTimer += Time.unscaledDeltaTime;

            if (!isCapturing)
            {
                if (runTimer >= warmupSeconds)
                {
                    isCapturing = true;
                    captureTimer = 0f;
                    sampleTimer = 0f;
                    frameTimer = 0f;
                    frameCount = 0;
                    samples.Clear();
                    Debug.Log("[MiniFpsBenchmarkLogger] Warm-up selesai. Capture FPS dimulai.");
                }
                else
                {
                    return;
                }
            }

            float deltaTime = Time.unscaledDeltaTime;
            captureTimer += deltaTime;
            sampleTimer += deltaTime;
            frameTimer += deltaTime;
            frameCount++;

            float sampleInterval = Mathf.Max(0.1f, sampleIntervalSeconds);
            if (sampleTimer >= sampleInterval)
            {
                SaveCurrentSample();
            }

            if (captureTimer >= captureSeconds)
            {
                StopAndSave();
            }
        }

        [ContextMenu("Begin Mini Benchmark Run")]
        public void BeginRun()
        {
            outputFolderPath = Path.Combine(Application.persistentDataPath, outputFolderName);
            Directory.CreateDirectory(outputFolderPath);

            startedAt = DateTime.Now;
            isRunning = true;
            isCapturing = false;
            runTimer = 0f;
            captureTimer = 0f;
            sampleTimer = 0f;
            frameTimer = 0f;
            frameCount = 0;
            samples.Clear();

            Debug.Log("[MiniFpsBenchmarkLogger] Run dimulai. Folder output: " + outputFolderPath);
        }

        [ContextMenu("Stop And Save Mini Benchmark")]
        public void StopAndSave()
        {
            if (!isRunning)
            {
                return;
            }

            if (isCapturing && frameCount > 0 && frameTimer > 0f)
            {
                SaveCurrentSample();
            }

            isRunning = false;
            isCapturing = false;

            if (samples.Count > 0)
            {
                WriteCsv();
            }
        }

        private void SaveCurrentSample()
        {
            if (frameTimer <= 0f)
            {
                return;
            }

            samples.Add(new FpsSample
            {
                secondIndex = samples.Count + 1,
                durationSeconds = frameTimer,
                frameCount = frameCount,
                avgFps = frameCount / frameTimer
            });

            sampleTimer = 0f;
            frameTimer = 0f;
            frameCount = 0;
        }

        private void WriteCsv()
        {
            string timestamp = startedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string sceneName = SceneManager.GetActiveScene().name;
            string safeScenario = MakeSafeToken(scenarioName);
            string safeScene = MakeSafeToken(sceneName);
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_{3}.csv",
                MakeSafeToken(outputFilePrefix),
                safeScene,
                safeScenario,
                timestamp);

            string filePath = Path.Combine(outputFolderPath, fileName);

            float totalDuration = 0f;
            int totalFrames = 0;
            float minFps = float.MaxValue;
            float maxFps = 0f;

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("Scene,Scenario,RunStartedAt,SampleIndex,SampleDurationSeconds,FrameCount,AverageFPS");

                for (int i = 0; i < samples.Count; i++)
                {
                    FpsSample sample = samples[i];
                    totalDuration += sample.durationSeconds;
                    totalFrames += sample.frameCount;
                    minFps = Mathf.Min(minFps, sample.avgFps);
                    maxFps = Mathf.Max(maxFps, sample.avgFps);

                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4:F4},{5},{6:F4}",
                        EscapeCsv(sceneName),
                        EscapeCsv(scenarioName),
                        startedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        sample.secondIndex,
                        sample.durationSeconds,
                        sample.frameCount,
                        sample.avgFps));
                }

                float overallFps = totalDuration > 0f ? totalFrames / totalDuration : 0f;
                writer.WriteLine();
                writer.WriteLine("SummaryKey,SummaryValue");
                writer.WriteLine("Scene," + EscapeCsv(sceneName));
                writer.WriteLine("Scenario," + EscapeCsv(scenarioName));
                writer.WriteLine("RunStartedAt," + startedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteLine("SampleCount," + samples.Count);
                writer.WriteLine("CaptureSeconds," + totalDuration.ToString("F4", CultureInfo.InvariantCulture));
                writer.WriteLine("TotalFrames," + totalFrames);
                writer.WriteLine("AverageFPS," + overallFps.ToString("F4", CultureInfo.InvariantCulture));
                writer.WriteLine("MinFPS," + minFps.ToString("F4", CultureInfo.InvariantCulture));
                writer.WriteLine("MaxFPS," + maxFps.ToString("F4", CultureInfo.InvariantCulture));
            }

            Debug.Log("[MiniFpsBenchmarkLogger] CSV tersimpan di: " + filePath);
        }

        private static string MakeSafeToken(string value)
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
    }
}
