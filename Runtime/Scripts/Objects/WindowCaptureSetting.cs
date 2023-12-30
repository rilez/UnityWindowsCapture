using System;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    [Serializable]
    public class WindowCaptureSetting
    {
        public WindowTitle WindowTitle;
        public int MonitorID = -1;
        [Range(.01f, 60f)]
        public float FrameRate;
        public bool SetWindowCaptureDataInSeparateThread = true;
    }
}