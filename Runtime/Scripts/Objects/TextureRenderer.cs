using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityWindowsCapture.Runtime
{
    [Serializable]
    public class TextureRenderer
    {
        public Transform Transform;
        public int MaterialIndex;
        public bool MaintainAspectRatio = true;
        public Action<Texture2D, int> SetTexture;
        private static readonly List<TextureRendererHandlerBase> actions;

        static TextureRenderer()
        {
            actions = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.IsSubclassOf(typeof(TextureRendererHandlerBase)) && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => Activator.CreateInstance(t) as TextureRendererHandlerBase)
                .ToList();
        }

        public void Initialize()
        {
            foreach (var action in actions)
            {
                var component = Transform.GetComponent(action.ComponentType);

                if (component == null) continue;

                SetTexture = action.GetAction(component);
                return;
            }

            Debug.LogError($"No Renderer component found on {Transform.name}");
        }
    }
}