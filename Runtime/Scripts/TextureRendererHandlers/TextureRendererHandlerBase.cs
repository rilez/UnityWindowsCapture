using System;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    public abstract class TextureRendererHandlerBase
    {
        public abstract Type ComponentType { get; }
        public abstract Action<Texture2D, int> GetAction(Component component);
    }
}