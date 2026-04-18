using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HackKU.TTS
{
    // Looks up keys in (1) process env vars, (2) <projectRoot>/.env, (3) StreamingAssets/.env.
    // For shipping builds, copy the .env file into Assets/StreamingAssets/ (it is gitignored).
    public static class EnvLoader
    {
        private static readonly Dictionary<string, CachedFile> _fileCache = new();

        private class CachedFile
        {
            public DateTime LastWriteUtc;
            public Dictionary<string, string> Values;
        }

        public static bool TryGet(string key, out string value)
        {
            value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value)) return true;

            foreach (var path in CandidatePaths())
            {
                var values = LoadFile(path);
                if (values != null && values.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                    return true;
            }

            value = null;
            return false;
        }

        public static string Get(string key)
        {
            return TryGet(key, out var v) ? v : null;
        }

        private static IEnumerable<string> CandidatePaths()
        {
            // Application.dataPath is <project>/Assets in Editor and <build>_Data at runtime.
            // The project-root fallback works in Editor and any dev run from the project folder.
            string dataPath = Application.dataPath;
            if (!string.IsNullOrEmpty(dataPath))
                yield return Path.GetFullPath(Path.Combine(dataPath, "..", ".env"));

            string streaming = Application.streamingAssetsPath;
            if (!string.IsNullOrEmpty(streaming))
                yield return Path.Combine(streaming, ".env");
        }

        private static Dictionary<string, string> LoadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var lastWrite = File.GetLastWriteTimeUtc(path);
                if (_fileCache.TryGetValue(path, out var cached) && cached.LastWriteUtc == lastWrite)
                    return cached.Values;

                var values = Parse(File.ReadAllLines(path));
                _fileCache[path] = new CachedFile { LastWriteUtc = lastWrite, Values = values };
                return values;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EnvLoader] Failed to read '{path}': {ex.Message}");
                return null;
            }
        }

        private static Dictionary<string, string> Parse(string[] lines)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.StartsWith("#")) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                dict[key] = value;
            }
            return dict;
        }
    }
}
