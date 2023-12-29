using System;
using System.Runtime.InteropServices;

namespace UnityWindowsCapture.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        
        public override bool Equals(object obj)
        {
            if (obj is RECT other)
            {
                return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + Left.GetHashCode();
            hash = hash * 23 + Top.GetHashCode();
            hash = hash * 23 + Right.GetHashCode();
            hash = hash * 23 + Bottom.GetHashCode();
            return hash;
        }
    }
}