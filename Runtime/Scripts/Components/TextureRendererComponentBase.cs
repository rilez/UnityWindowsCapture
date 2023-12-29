using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityWindowsCapture.Runtime
{
    public abstract class TextureRendererComponentBase : MonoBehaviour
    {
        [FormerlySerializedAs("windowCapture")] public WindowCapturer windowCapturer;
        [ReadOnly]
        public float FrameUpdateTimer;
        public float RetryInitializationTime = 1f;
        [ReadOnly]
        public float RetryInitializationTimer;
        public string SettingsFilename;
        public virtual string SettingsConfigurationSubdirectory => "Host";
        private ISettingsHandler settingsHandler; 

        public virtual void OnEnable()
        {
            FrameUpdateTimer = 0f;
            RetryInitializationTimer = 0f;
            settingsHandler = GetSettingsHandler();

            if (settingsHandler == null) return;
            
            DeserializeSettings();

            if (windowCapturer.windowCaptureSetting == null) return;
            
            windowCapturer.Initialize();
        }

        public virtual void Update()
        {
            if (!ShouldUpdate(windowCapturer.windowCaptureSetting == null)) return;

            if (!ShouldUpdate(!windowCapturer.HasValidInitialization())) return;
            
            FrameUpdateTimer += Time.deltaTime;

            if (!(FrameUpdateTimer > windowCapturer.FrameUpdateInterval)) return;
            
            FrameUpdateTimer = 0f;
            Execute();
            
            if (windowCapturer.HasValidData()) return;
            
            windowCapturer.Cleanup();
        }
        
        public virtual void OnDisable()
        {
            windowCapturer.Cleanup();
        }
        
        private ISettingsHandler GetSettingsHandler()
        {
            var settingsHandlerType =  AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => typeof(ISettingsHandler).IsAssignableFrom(t))
                .OrderByDescending(t => ((PriorityAttribute)Attribute.GetCustomAttribute(t, typeof(PriorityAttribute)))?.Priority ?? 0)
                .FirstOrDefault();
            
            if (settingsHandlerType != null)
            {
                return (ISettingsHandler)Activator.CreateInstance(settingsHandlerType);
            }
        
            Debug.LogError($"No {typeof(ISettingsHandler).Name} implementation found!.");
            return null;
        }

        private bool ShouldUpdate(bool predicate)
        {
            if (!predicate) return true;
            
            RetryInitializationTimer += Time.deltaTime;
                
            if (!(RetryInitializationTimer > RetryInitializationTime)) return false;
                
            FrameUpdateTimer = 0f;
            RetryInitializationTimer = 0f;
            windowCapturer.Cleanup();
            DeserializeSettings();

            if (windowCapturer.windowCaptureSetting == null) return false;
                
            windowCapturer.Initialize();
            return false;

        }
        
        [ContextMenu("SerializeSettings")]
        private void SerializeSettings()
        {
            if (!Application.isPlaying)
            {
                settingsHandler = GetSettingsHandler();
            }

            settingsHandler?.SerializeSettings(windowCapturer.windowCaptureSetting, SettingsFilename, SettingsConfigurationSubdirectory);
        }
        
        [ContextMenu("DeserializeSettings")]
        private void DeserializeSettings()
        {
            var windowTitles = new List<string>(windowCapturer.windowCaptureSetting.WindowTitle.WindowTitles);

            if (!Application.isPlaying)
            {
                settingsHandler = GetSettingsHandler();
            }

            windowCapturer.windowCaptureSetting = settingsHandler?.DeserializeSettings<WindowCaptureSetting>(SettingsFilename, SettingsConfigurationSubdirectory);

            if (windowCapturer.windowCaptureSetting == null) return;
            
            windowCapturer.windowCaptureSetting.WindowTitle.WindowTitles = windowTitles;
        }

        public abstract void Execute();
    }
}