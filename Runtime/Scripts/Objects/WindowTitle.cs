using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityWindowsCapture.Runtime
{
    [Serializable]
    public struct WindowTitle
    {
        public string Value;
        [JsonIgnore]
        public List<string> WindowTitles;
    }
}