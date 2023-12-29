using System;

namespace UnityWindowsCapture.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; set; }

        public PriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}