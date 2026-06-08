using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool untuk mereset progres quiz dari menu bar Unity.
/// Gunakan ini saat testing untuk membersihkan data PlayerPrefs lama.
/// Menu: Tools → Quiz → Reset Quiz Progress
/// </summary>
public static class QuizProgressResetTool
{
    private const string KeyWavePassedPrefix = "QUIZ_WAVE_PASSED_";
    private static readonly int[] KnownWaveNumbers = { 1, 2, 3 };

    [MenuItem("Tools/Quiz/Reset Quiz Progress (Testing)")]
    public static void ResetQuizProgress()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Reset Quiz Progress",
            "Ini akan menghapus semua data progres quiz dari PlayerPrefs.\n\n" +
            "Gunakan hanya untuk keperluan testing!\n\n" +
            "Lanjutkan?",
            "Reset", "Batal");

        if (!confirm) return;

        int deleted = 0;
        foreach (int waveNum in KnownWaveNumbers)
        {
            string key = KeyWavePassedPrefix + waveNum;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                deleted++;
                Debug.Log($"[QuizReset] Dihapus: {key}");
            }
        }

        PlayerPrefs.Save();

        if (deleted == 0)
            Debug.Log("[QuizReset] Tidak ada data progres quiz yang ditemukan. Sudah bersih.");
        else
            Debug.Log($"[QuizReset] Berhasil menghapus {deleted} key progres quiz. Game sekarang kembali ke kondisi awal.");

        EditorUtility.DisplayDialog(
            "Reset Selesai",
            deleted == 0
                ? "Tidak ada data quiz yang perlu dihapus."
                : $"Berhasil menghapus {deleted} key progres quiz.\nGame akan mulai dari awal saat Play Mode dijalankan.",
            "OK");
    }

    [MenuItem("Tools/Quiz/Show Current Quiz Progress")]
    public static void ShowQuizProgress()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[QuizProgress] State PlayerPrefs saat ini:");
        foreach (int waveNum in KnownWaveNumbers)
        {
            string key = KeyWavePassedPrefix + waveNum;
            bool passed = PlayerPrefs.GetInt(key, 0) == 1;
            sb.AppendLine($"  Wave {waveNum}: {(passed ? "SUDAH LULUS" : "belum lulus")}");
        }
        Debug.Log(sb.ToString());
    }
}
