using System;

namespace UnityWindowsCapture.Runtime
{
    // TODO Merge in with ICaptureData/MonitorCaptureData
    // TODO Generalize/make WindowCapture logic/naming generic
    [Serializable]
    public struct CaptureData
    {
        public int Width;
        public int Height;
        public byte[] Data;
        
        public override bool Equals(object obj)
        {
            if (obj is CaptureData other)
            {
                return Width == other.Width && Height == other.Height && Data.Equals(other.Data);
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + Width.GetHashCode();
            hash = hash * 23 + Height.GetHashCode();
            hash = hash * 23 + Data.GetHashCode();
            return hash;
        }
    }
}