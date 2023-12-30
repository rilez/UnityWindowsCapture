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
        
        
        // /// <summary>
        // /// Gets the list of monitors.
        // /// </summary>
        // static Dictionary<string, MONITORINFOEX> GetMonitors()
        // {
        //     EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, **MonitorEnumProc**, IntPtr.Zero);
        //
        //     foreach (var item in monitors)
        //     {
        //         Debug.Log($"{item.Value.szDevice}, Resolution:  
        //         {item.Value.rcMonitor.Right - item.Value.rcMonitor.Left}, 
        //         {item.Value.rcMonitor.Left- item.Value.rcMonitor.Top}, Pos: 
        //         {item.Value.rcMonitor.Left},{item.Value.rcMonitor.Top}");
        //     }
        //
        //     return monitors;
        // }
        //
        // /// <summary>
        // /// Callback during listing of monitors. Invoked for ever attached display.
        // /// After last invoke, the method EnumDisplayMonitors returns.
        // /// </summary>
        // private static bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rectangle lprcMonitor, IntPtr dwData)
        // {
        //     MONITORINFOEX monitorInfo = new MONITORINFOEX();
        //     monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
        //     bool res = GetMonitorInfo(hMonitor,  ref monitorInfo);
        //     if (res == false)
        //     {                 
        //         var err = Marshal.GetLastWin32Error();
        //     }            
        //
        //     return true;
        // }
        //
        // [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        // public struct MONITORINFOEX
        // {
        //     public int cbSize;
        //     public Rectangle rcMonitor;
        //     public Rectangle rcWork;
        //     public uint dwFlags;
        //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        //     public string szDevice;
        // }
        //
        // [DllImport("user32.dll")]
        // [return: MarshalAs(UnmanagedType.Bool)]
        // public static extern bool EnumDisplayMonitors(IntPtr hdc,
        //     IntPtr lprcClip,
        //     EnumMonitorsDelegate lpfnEnum,
        //     IntPtr dwData);
        
        
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
            var screenBounds = System.Windows.Forms.Screen.AllScreens // Get all screens
                .Select(screen => screen.Bounds) // Get bounds of all screens
                .Aggregate(Rectangle.Union); // Union all bounds to get a rectangle that covers them all

            var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, bitmap.Size);
            }

            // return bitmap;
            
            // GetWindowRect(hWnd, out var rect);
            var width = screenBounds.Width;
            // var width = rect.Right - rect.Left;
            var height = screenBounds.Height;
            // var height = rect.Bottom - rect.Top;

            var imageSize = Math.Abs(height * width * 4);
            
            if (windowCaptureData.Data?.Length != imageSize)
            {
                windowCaptureData.Data = new byte[imageSize];
            }
            
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            
            windowCaptureData.Width = width;
            windowCaptureData.Height = height;
            
            Marshal.Copy(bitmapData.Scan0, windowCaptureData.Data, 0, imageSize);
            
            bitmap.UnlockBits(bitmapData);
    
            // try
            // {
            //     var hdcSrc = GetWindowDC(hWnd);
            //
            //     using (var bmp = new Bitmap(width, height))
            //     {
            //         using (var g = Graphics.FromImage(bmp))
            //         {
            //             var hdcDest = g.GetHdc();
            //             BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, 0x00CC0020);
            //             g.ReleaseHdc(hdcDest);
            //         }
            //
            //         var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            //
            //         windowCaptureData.Width = width;
            //         windowCaptureData.Height = height;
            //
            //         Marshal.Copy(bitmapData.Scan0, windowCaptureData.Data, 0, imageSize);
            //
            //         bmp.UnlockBits(bitmapData);
            //     }
            //
            //     ReleaseDC(hWnd, hdcSrc);
            // }
            // catch (Exception e)
            // {
            //     Debug.LogWarning("Could not get texture for window...");
            // }
    
            return windowCaptureData;
        }
        
        public static Bitmap CaptureScreen()
        {
            var screenBounds = System.Windows.Forms.Screen.AllScreens // Get all screens
                .Select(screen => screen.Bounds) // Get bounds of all screens
                .Aggregate(Rectangle.Union); // Union all bounds to get a rectangle that covers them all

            var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, bitmap.Size);
            }

            return bitmap;
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