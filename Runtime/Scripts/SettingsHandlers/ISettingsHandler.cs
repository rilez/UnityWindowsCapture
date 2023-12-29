namespace UnityWindowsCapture.Runtime
{
    public interface ISettingsHandler
    {
        void SerializeSettings<T>(T data, string fileName, string subdirectory = "", bool useStreamingAssetsDirectory = true);
        T DeserializeSettings<T>(string fileName = "", string subdirectory = "", bool useStreamingAssetsDirectory = true) where T : new();
    }
}