using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
        
        public async static Task<WindowCaptureData> GetWindowCaptureData(IntPtr hWnd, WindowCaptureData windowCaptureData)
        {
            GetWindowRect(hWnd, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            var imageSize = Math.Abs(height * width * 4);
            
            if (windowCaptureData.Data?.Length != imageSize)
            {
                windowCaptureData.Data = new byte[imageSize];
            }
    
            try
            {
                var hdcSrc = GetWindowDC(hWnd);

                using (var bmp = new Bitmap(width, height))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        var hdcDest = g.GetHdc();
                        BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, 0x00CC0020);
                        g.ReleaseHdc(hdcDest);
                    }

                    var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            
                    windowCaptureData.Width = width;
                    windowCaptureData.Height = height;
            
                    Marshal.Copy(bitmapData.Scan0, windowCaptureData.Data, 0, imageSize);
            
                    bmp.UnlockBits(bitmapData);
                }

                ReleaseDC(hWnd, hdcSrc);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not get texture for window...");
            }
    
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