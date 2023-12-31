using System;

namespace UnityWindowsCapture.Runtime
{
    public class MonitorCaptureData : ICaptureData
    {
        public CaptureDataType DataType => CaptureDataType.Monitor;
        public IntPtr Handle { get; }
        public string Description { get; }
        public MonitorInfoEx Info;
        public bool IsCapturable() => true;

        public MonitorCaptureData(IntPtr handle)
        {
            Handle = handle;
            NativeAPI.UpdateMonitorInfo(Handle, ref Info);
            Description = Info.DeviceName;
        }
    }
}