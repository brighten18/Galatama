using System;
using System.IO;
using UnityEngine;

namespace GALATAMA.MainMenu
{
    public class SimplePlayerPositionSave : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private KeyCode loadKey = KeyCode.F9;
        [SerializeField] private string fileName = "player_position.json";

        [Serializable]
        private class PlayerPositionData
        {
            public float x;
            public float y;
            public float z;
        }

        private void Update()
        {
            if (Input.GetKeyDown(saveKey))
            {
                SavePosition();
            }

            if (Input.GetKeyDown(loadKey))
            {
                LoadPosition();
            }
        }

        [ContextMenu("Save Position")]
        public void SavePosition()
        {
            if (player == null)
            {
                Debug.LogWarning("[SimplePlayerPositionSave] Player belum di-assign.");
                return;
            }

            PlayerPositionData data = new PlayerPositionData
            {
                x = player.position.x,
                y = player.position.y,
                z = player.position.z
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetFilePath(), json);

            Debug.Log("[SimplePlayerPositionSave] Posisi tersimpan ke: " + GetFilePath());
        }

        [ContextMenu("Load Position")]
        public void LoadPosition()
        {
            if (player == null)
            {
                Debug.LogWarning("[SimplePlayerPositionSave] Player belum di-assign.");
                return;
            }

            string path = GetFilePath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SimplePlayerPositionSave] File save belum ada: " + path);
                return;
            }

            string json = File.ReadAllText(path);
            PlayerPositionData data = JsonUtility.FromJson<PlayerPositionData>(json);
            if (data == null)
            {
                Debug.LogWarning("[SimplePlayerPositionSave] Data save tidak valid.");
                return;
            }

            player.position = new Vector3(data.x, data.y, data.z);
            Debug.Log("[SimplePlayerPositionSave] Posisi player berhasil dimuat.");
        }

        private string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
