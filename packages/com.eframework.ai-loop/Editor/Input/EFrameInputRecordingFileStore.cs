#if EFRAME_AI_LOOP_HAS_INPUT_SYSTEM
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EFramework.Editor.AILoop
{
    internal static class EFrameInputRecordingFileStore
    {
        public static readonly string DefaultDirectory =
            Path.Combine(".eframe", "outputs", "InputRecordings");

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented
        };

        public static string ResolveOutputPath(string outputPath)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                return outputPath;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            return Path.Combine(DefaultDirectory, $"{timestamp}.json");
        }

        public static string ResolveInputPath(string inputPath)
        {
            if (!string.IsNullOrWhiteSpace(inputPath))
            {
                return inputPath;
            }

            if (!Directory.Exists(DefaultDirectory))
            {
                return "";
            }

            return Directory.GetFiles(DefaultDirectory, "*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? "";
        }

        public static void Save(EFrameInputRecordingData data, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(data, JsonSettings));
        }

        public static EFrameInputRecordingData Load(string path)
        {
            return JsonConvert.DeserializeObject<EFrameInputRecordingData>(File.ReadAllText(path));
        }

        public static string FormatVector2(Vector2 value)
        {
            return value.x.ToString(CultureInfo.InvariantCulture) + "," +
                   value.y.ToString(CultureInfo.InvariantCulture);
        }

        public static Vector2 ParseVector2(string data)
        {
            string[] parts = data.Split(',');
            if (parts.Length != 2)
            {
                return Vector2.zero;
            }

            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
            return new Vector2(x, y);
        }

        public static Key[] ParseKeyFilter(string keys)
        {
            if (string.IsNullOrWhiteSpace(keys))
            {
                return null;
            }

            return keys.Split(',')
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .Select(value => Enum.TryParse(value, true, out Key key) ? key : Key.None)
                .Where(key => key != Key.None)
                .Distinct()
                .ToArray();
        }
    }
}
#endif
