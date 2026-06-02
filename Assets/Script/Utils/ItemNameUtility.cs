using System.Text.RegularExpressions;

public static class ItemNameUtility
{
    // Hapus semua variasi "(Clone)" yang Unity generate
    public static string CleanName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return rawName;

        // Hapus semua " (Clone)" atau "(Clone)" sebanyak apapun
        string cleaned = Regex.Replace(rawName, @"\s*\(Clone\)", "");
        return cleaned.Trim();
    }
}
