using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityWindowsCapture.Runtime
{
    [Serializable]
    public class WindowCapturer
    {
        [FormerlySerializedAs("windowCapturerSetting")] [FormerlySerializedAs("WindowCaptureSetting")] public WindowCaptureSetting windowCaptureSetting;
        public float FrameUpdateInterval => 1f / frameRateClamped;
        private float frameRateClamped
        {
            get => windowCaptureSetting.FrameRate;
            set => windowCaptureSetting.FrameRate = Mathf.Clamp(value, 0.01f, 60f);
        }
        [NonSerialized]
        public WindowCaptureData[] windowCaptureDatasCurrent;
        private WindowCaptureData[] windowCaptureDatasPrevious;
        private Texture2D texture;
        private IntPtr targetWindowHandle;
        private CancellationTokenSource cancellationTokenSource;
        private Task setWindowCaptureDataTask;
        
        public void Initialize()
        {
            windowCaptureDatasCurrent = new WindowCaptureData[1];
            targetWindowHandle = NativeAPI.FindWindow(null, windowCaptureSetting.WindowTitle.Value);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async void Cleanup()
        {
            if(TaskIsRunning(setWindowCaptureDataTask))
            {
                await StopSetWindowCaptureDataTask();
            }
            
            targetWindowHandle = IntPtr.Zero;
        }
        
        public bool HasValidInitialization()
        {
            return targetWindowHandle != IntPtr.Zero;
        }

        public bool HasValidData()
        {
            return windowCaptureDatasCurrent[0].Data?.Length > 0;
        }

        public async Task<Texture2D> SetTextureDataAsync(bool setWindowCaptureDataInSeparateThread = true)
        {
            if(setWindowCaptureDataInSeparateThread)
            {
                if(!TaskIsRunning(setWindowCaptureDataTask))
                {
                    setWindowCaptureDataTask = RunSetWindowCaptureDataTask(cancellationTokenSource.Token);
                }

                return GetTexture();
            }
   
            if(TaskIsRunning(setWindowCaptureDataTask))
            {
                await StopSetWindowCaptureDataTask();
            }
    
            windowCaptureDatasCurrent[0] = await NativeAPI.GetWindowCaptureData(targetWindowHandle, windowCaptureDatasCurrent[0]);
            return GetTexture();
        }
        
        private Texture2D GetTexture()
        {
            if (texture == null || texture.width != windowCaptureDatasCurrent[0].Width || texture.height != windowCaptureDatasCurrent[0].Height)
            {
                var width = Math.Max(windowCaptureDatasCurrent[0].Width, 1);
                var height = Math.Max(windowCaptureDatasCurrent[0].Height, 1);
                texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
            }

            if (windowCaptureDatasPrevious != null && windowCaptureDatasPrevious[0].Equals(windowCaptureDatasCurrent[0].Data))
            {
                return texture;
            }

            if (HasValidData())
            {
                texture.LoadRawTextureData(windowCaptureDatasCurrent[0].Data);
                texture.Apply(); 
            }
  
            windowCaptureDatasPrevious ??= new WindowCaptureData[1];
            windowCaptureDatasPrevious[0] = windowCaptureDatasCurrent[0];
            return texture;
        }
        
        private bool TaskIsRunning(Task task)
        {
            return task != null && task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted && task.Status != TaskStatus.Canceled;
        }
        
        private async Task RunSetWindowCaptureDataTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                windowCaptureDatasCurrent[0] = await NativeAPI.GetWindowCaptureData(targetWindowHandle, windowCaptureDatasCurrent[0]);
                
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(FrameUpdateInterval), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
        
        private async Task StopSetWindowCaptureDataTask()
        {
            cancellationTokenSource.Cancel();
            await setWindowCaptureDataTask;
            setWindowCaptureDataTask.Dispose();
        }
    }
}