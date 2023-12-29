using System;

namespace UnityWindowsCapture.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PriorityAttribute_Test : Attribute
    {
        public int Priority { get; set; }

        public PriorityAttribute_Test(int priority)
        {
            Priority = priority;
        }
    }
}