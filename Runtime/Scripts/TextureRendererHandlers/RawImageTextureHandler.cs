using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityWindowsCapture.Runtime
{
    public class RawImageTextureHandler : TextureRendererHandlerBase
    {
        public override Type ComponentType => typeof(RawImage);

        public override Action<Texture2D, int> GetAction(Component component)
        {
            var renderer = (RawImage)component;

            return (texture, _) =>
            {
                renderer.texture = texture;
            };  
        }
    }
}