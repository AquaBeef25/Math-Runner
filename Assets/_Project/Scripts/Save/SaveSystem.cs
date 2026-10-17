using System;
using System.IO;
using UnityEngine;

namespace MathRunner.Save
{
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static string _path;
        private static SaveData _data;

        public static string FilePath => _path ??= Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data => _data ??= Load();

        // Points saving at another file and forgets the cached data. Tests use this so they never touch the real save.
        public static void UseFile(string path)
        {
            _path = path;
            _data = null;
        }

        public static void Save()
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }

        private static SaveData Load()
        {
            if (!File.Exists(FilePath)) return new SaveData();
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save file at {FilePath} could not be read and was reset: {e.Message}");
                return new SaveData();
            }
        }
    }
}
