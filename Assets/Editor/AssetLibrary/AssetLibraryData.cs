using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetLibrary
{
    public enum AssetKind { Texture, Audio, Model, Unknown }

    [Serializable]
    public class AssetEntry
    {
        public string guid;
        public string assetPath;   // Assets/...
        public string name;
        public AssetKind kind;
        public List<string> tags = new();
        public string category = "";
        public string md5 = "";
        public long fileSizeBytes;
        public string addedAt;
    }

    [Serializable]
    public class AssetLibraryDatabase
    {
        public List<AssetEntry> entries = new();
    }

    public static class AssetLibraryData
    {
        static string SavePath => Path.Combine(
            Application.persistentDataPath, "AssetLibraryDB.json");

        public static AssetLibraryDatabase Load()
        {
            if (!File.Exists(SavePath)) return new AssetLibraryDatabase();
            var json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<AssetLibraryDatabase>(json)
                   ?? new AssetLibraryDatabase();
        }

        public static void Save(AssetLibraryDatabase db)
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(db, true));
        }
    }
}