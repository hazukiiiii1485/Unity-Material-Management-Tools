using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace AssetLibrary
{
    public static class AssetScanner
    {
        static readonly string[] TextureExt = { "png","jpg","jpeg","tga","psd","bmp","gif","hdr","exr" };
        static readonly string[] AudioExt   = { "mp3","wav","ogg","aiff","aif","flac" };
        static readonly string[] ModelExt   = { "fbx","obj","dae","blend","3ds","prefab" };

        public static AssetKind DetectKind(string assetPath)
        {
            var ext = Path.GetExtension(assetPath).TrimStart('.').ToLower();
            if (Array.Exists(TextureExt, e => e == ext)) return AssetKind.Texture;
            if (Array.Exists(AudioExt,   e => e == ext)) return AssetKind.Audio;
            if (Array.Exists(ModelExt,   e => e == ext)) return AssetKind.Model;
            return AssetKind.Unknown;
        }

        public static AssetEntry ScanEntry(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var fullPath = Path.GetFullPath(assetPath);
            var fi = new FileInfo(fullPath);

            return new AssetEntry
            {
                guid        = guid,
                assetPath   = assetPath,
                name        = Path.GetFileNameWithoutExtension(assetPath),
                kind        = DetectKind(assetPath),
                md5         = ComputeMD5(fullPath),
                fileSizeBytes = fi.Exists ? fi.Length : 0,
                addedAt     = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };
        }

        public static string ComputeMD5(string fullPath)
        {
            try {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(fullPath);
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","");
            } catch { return ""; }
        }
    }
}
