using System;
using System.IO;
using UnityEngine;

namespace GALATAMA.MainMenu
{
    [Serializable]
    public class SaveGameData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public long savedAtTicks;
    }

    public static class SaveGameService
    {
        private const string SaveFileName = "savegame.json";

        public static string SaveFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void Save(SaveGameData data)
        {
            if (data == null)
            {
                Debug.LogError("SaveGameService.Save gagal: data null.");
                return;
            }

            data.savedAtTicks = DateTime.UtcNow.Ticks;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log("Save tersimpan di: " + SaveFilePath);
        }

        public static bool TryLoad(out SaveGameData data)
        {
            data = null;

            if (!HasSave())
            {
                return false;
            }

            string json = File.ReadAllText(SaveFilePath);
            data = JsonUtility.FromJson<SaveGameData>(json);
            return data != null;
        }

        public static void DeleteSave()
        {
            if (!HasSave())
            {
                return;
            }

            File.Delete(SaveFilePath);
            Debug.Log("Save dihapus: " + SaveFilePath);
        }
    }
}
