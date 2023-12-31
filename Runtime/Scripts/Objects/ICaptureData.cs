using System;

namespace UnityWindowsCapture.Runtime
{
    public interface ICaptureData
    {
        CaptureDataType DataType { get; }
        IntPtr Handle { get; }
        string Description { get; }
        bool IsCapturable();
    }
}