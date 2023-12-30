using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOT;
using UnityEngine;
using Graphics = System.Drawing.Graphics;

namespace UnityWindowsCapture.Runtime
{
    public static class NativeAPI
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string windowClassName, string windowTitleName);
        
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
        
        [MonoPInvokeCallback(typeof(EnumWindowsProc))]
        public static List<string> GetAllWindowTitles()
        {
            var windowTitles = new List<string>();
            
            EnumWindows((hWnd, lParam) =>
            {
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);
                var windowTitle = sb.ToString();

                if (!string.IsNullOrEmpty(windowTitle) && !windowTitles.Contains(windowTitle))
                {
                    windowTitles.Add(windowTitle);
                }

                return true;

            }, IntPtr.Zero);

            return windowTitles;
        }
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        // [DllImport("user32.dll")]
        // public static extern IntPtr GetWindowDC(IntPtr hWnd);

        public static async Task<WindowCaptureData> GetWindowCaptureData(IntPtr hWnd, WindowCaptureData windowCaptureData, int monitorIndex = -1)
        {
            RECT rect;
            IntPtr hdcSrc;
    
            if (monitorIndex >= 0) // For specific monitor capture
            {
                if (monitorIndex >= System.Windows.Forms.Screen.AllScreens.Length)
                    throw new ArgumentOutOfRangeException("monitorIndex");

                var screen = System.Windows.Forms.Screen.AllScreens[monitorIndex];
                rect = new RECT() 
                { 
                    Left = screen.Bounds.Left, 
                    Top = screen.Bounds.Top, 
                    Right = screen.Bounds.Right, 
                    Bottom = screen.Bounds.Bottom 
                };

                hdcSrc = GetDC(IntPtr.Zero); // get a DC for the entire screen
            }
            else // For specific window capture.
            {
                GetWindowRect(hWnd, out rect);
                hdcSrc = GetWindowDC(hWnd); // get a DC for the specific window
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            var bitmap = new Bitmap(width, height);

            using (var gfx = Graphics.FromImage(bitmap))
            {
                var hdcDest = gfx.GetHdc();
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, rect.Left, rect.Top, 0x00CC0020);
                gfx.ReleaseHdc(hdcDest);
            }

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var imageSize = Math.Abs(height * width * 4);

            if (windowCaptureData.Data?.Length != imageSize)
            {
                windowCaptureData.Data = new byte[imageSize];
            }

            windowCaptureData.Width = width;
            windowCaptureData.Height = height;

            Marshal.Copy(bitmapData.Scan0, windowCaptureData.Data, 0, imageSize);
            bitmap.UnlockBits(bitmapData);
            ReleaseDC(hWnd, hdcSrc);

            return windowCaptureData;
        }
        
        public static Texture2D FlipTextureVertically(Texture2D original)
        {
            var originalPixels = original.GetRawTextureData<Color32>();
          
            var height = original.height;
            var width = original.width;
         
            var pLength = width * height;
            var flippedPixels = new Unity.Collections.NativeArray<Color32>(pLength, Unity.Collections.Allocator.Temp);
          
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    flippedPixels[y * width + x] = originalPixels[(height - y - 1) * width + x];
                }
            }

            var flipped = new Texture2D(width, height, TextureFormat.BGRA32, false);
            flipped.LoadRawTextureData(flippedPixels);
            flipped.Apply(updateMipmaps: false);
          
            flippedPixels.Dispose();
       
            return flipped;
        }
    }
}