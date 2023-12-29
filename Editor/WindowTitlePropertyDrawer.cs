using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityWindowsCapture.Runtime;

namespace UnityWindowsCapture.Editor
{
    [CustomPropertyDrawer(typeof(WindowTitle))]
    public class WindowTitlePropertyDrawer : PropertyDrawer
    {
        private int windowTitleIndex;
        private List<string> windowTitles;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Application.isPlaying)
            {
                var valuePropertyRO = property.FindPropertyRelative("Value");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Value: ", valuePropertyRO.stringValue);
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            var windowTitlesProperty = property.FindPropertyRelative("WindowTitles");

            if (windowTitlesProperty.arraySize <= 0)
            {
                SetWindowTitlesProperty(windowTitlesProperty);
            }
            else
            {
                windowTitles = new List<string>();

                for (var i = 0; i < windowTitlesProperty.arraySize; i++)
                {
                    windowTitles.Add(windowTitlesProperty.GetArrayElementAtIndex(i).stringValue);
                } 
            }
            
            var valueProperty = property.FindPropertyRelative("Value");
            
            windowTitleIndex = windowTitles.Contains(valueProperty.stringValue)
                ? Array.IndexOf(windowTitles.ToArray(), valueProperty.stringValue)
                : 0;
           
            EditorGUILayout.BeginHorizontal();

            var windowTitleIndexTemp = EditorGUILayout.Popup("Window Title:", windowTitleIndex, windowTitles.ToArray());
            
            if (windowTitleIndexTemp != windowTitleIndex)
            {
                windowTitleIndex = windowTitleIndexTemp;
                valueProperty.stringValue = windowTitles.ToArray()[windowTitleIndex];
            }
            
            if(GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), GUILayout.Width(75)))
            {
                SetWindowTitlesProperty(windowTitlesProperty);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void SetWindowTitlesProperty(SerializedProperty windowTitlesProperty)
        {
            // if (Application.isPlaying) return;
            
            windowTitles = GetWindowTitles();
            windowTitlesProperty.ClearArray();
            windowTitlesProperty.arraySize = windowTitles.Count;
                
            for (var i = 0; i < windowTitles.Count; i++)
            {
                windowTitlesProperty.GetArrayElementAtIndex(i).stringValue = windowTitles[i];
            }
        }

        private List<string> GetWindowTitles()
        {
            return NativeAPI.GetAllWindowTitles().OrderBy(t => t).ToList();
        }
    }
}