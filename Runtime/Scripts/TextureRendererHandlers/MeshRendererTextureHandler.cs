using System;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    public class MeshRendererTextureHandler : TextureRendererHandlerBase
    {
        public override Type ComponentType => typeof(MeshRenderer);

        public override Action<Texture2D, int> GetAction(Component component)
        {
            var renderer = (MeshRenderer)component;
        
            return (texture, index) =>
            {
                if (renderer.materials.Length <= index) return;
            
                renderer.materials[index].mainTexture = texture;
            };   
        }
    }
}