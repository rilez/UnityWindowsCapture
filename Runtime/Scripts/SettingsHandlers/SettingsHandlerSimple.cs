using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    /// <summary>
    /// Simple implementation of ISettingsHandler
    /// To use your own, implement one with a higher priority
    /// </summary>
    [Priority(100)]
    public class SettingsHandlerSimple : ISettingsHandler
    {
        public void SerializeSettings<T>(T data, string fileName, string subdirectory, bool useStreamingAssetsDirectory = true)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var settingsDirectory = Path.Combine(Application.dataPath, "StreamingAssets", subdirectory, typeof(T).Name);
            Directory.CreateDirectory(settingsDirectory);
            File.WriteAllText(Path.Combine(settingsDirectory, fileName + ".json"), jsonData);
        }
    
        public T DeserializeSettings<T>(string fileName, string subdirectory, bool useStreamingAssetsDirectory = true) where T : new()
        {
            var path = Path.Combine(Application.dataPath, "StreamingAssets", subdirectory, typeof(T).Name, fileName + ".json");
            
            if (File.Exists(path))
            {
                var data = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(data);
            }

            Debug.LogWarning($"File at {path} could not be found. Could not deserialize settings.");
            return new T();
        }
    }
}