using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
// using System.Windows.Forms;
using AOT;
using UnityEngine;
using Graphics = System.Drawing.Graphics;

namespace UnityWindowsCapture.Runtime
{
    public static class NativeAPI
    {
        private static bool isEnumeratingMonitors;
        private static readonly List<IntPtr> enumeratedMonitors = new();
        
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [MonoPInvokeCallback(typeof(EnumMonitorsDelegate))]
        private static bool EnumDisplayMonitorsCallback(IntPtr monitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr data)
        {
            enumeratedMonitors.Add(monitor);
            return true;
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);
        
        public static IEnumerable<MonitorCaptureData> GetMonitors()
        {
            return EnumDisplayMonitors().Select(monitor => new MonitorCaptureData(monitor));
        }
        
        private static IEnumerable<IntPtr> EnumDisplayMonitors()
        {
            if(isEnumeratingMonitors) throw new InvalidOperationException("Only one instance of EnumDisplayMonitors() can be called at the same time.");
            
            isEnumeratingMonitors = true;
            enumeratedMonitors.Clear();
            
            try
            {
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumDisplayMonitorsCallback, IntPtr.Zero);
            }
            finally
            {
                isEnumeratingMonitors = false;
            }

            return enumeratedMonitors.ToArray();
        }
        
        public static void UpdateMonitorInfo(IntPtr handle, ref MonitorInfoEx info)
        {
            var mi = new MonitorInfoEx();
            mi.Size = Marshal.SizeOf(mi);
                
            if (GetMonitorInfo(handle, ref mi))
            {
                info = mi;
            }
        }

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
        // private static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
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
        
        public async static Task<CaptureData> GetWindowCaptureData(IntPtr hWnd, CaptureData captureData, int monitorID)
        {
            var rect = new RECT();
            
            if (monitorID >= 0 &&  monitorID < GetMonitors().ToList().Count)
            {
                var screenBounds = GetMonitors().ToList()[monitorID].Info.Monitor;
                rect.Left = screenBounds.Left;
                rect.Top = screenBounds.Top;
                rect.Right = screenBounds.Right;
                rect.Bottom = screenBounds.Bottom;
            }
            else
            {
                GetWindowRect(hWnd, out rect);
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            var imageSize = Math.Abs(height * width * 4);
    
            if (captureData.Data?.Length != imageSize)
            {   
                captureData.Data = new byte[imageSize];
            }

            try
            {
                using var bmp = new Bitmap(width, height);
                using var g = Graphics.FromImage(bmp);
                
                if (monitorID >= 0 && monitorID < GetMonitors().ToList().Count)
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(rect.Right - rect.Left, rect.Bottom - rect.Top));
                } 
                else 
                {
                    var hdcSrc = GetWindowDC(hWnd);
                    var hdcDest = g.GetHdc();
                    BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, 0x00CC0020);
                    g.ReleaseHdc(hdcDest);  
                }

                var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

                captureData.Width = width;
                captureData.Height = height;

                Marshal.Copy(bitmapData.Scan0, captureData.Data, 0, imageSize);

                bmp.UnlockBits(bitmapData);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not get texture...");
            }

            return captureData;
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