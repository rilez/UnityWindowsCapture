using System.Collections.Generic;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    public class TextureRendererComponent : TextureRendererComponentBase
    {
        public List<TextureRenderer> Data = new();

        public override void OnEnable()
        {
            base.OnEnable();

            foreach (var data in Data)
            {
                data.Initialize();
            }
        }

        public override async void Execute()
        {
            foreach (var data in Data)
            {
                data.SetTexture(await windowCapturer.SetTextureDataAsync(windowCapturer.windowCaptureSetting.SetWindowCaptureDataInSeparateThread), data.MaterialIndex);

                if (!data.MaintainAspectRatio) continue;
            
                var aspectRatio = (float)windowCapturer.windowCaptureDatasCurrent[0].Width / windowCapturer.windowCaptureDatasCurrent[0].Height;
                data.Transform.localScale = new Vector3(data.Transform.localScale.y * aspectRatio, data.Transform.localScale.y, data.Transform.localScale.z);
            }
        }
    }
}