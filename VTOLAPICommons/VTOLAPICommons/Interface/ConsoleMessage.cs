using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VTOLAPICommons
{
    public class ConsoleMessage
    {
        public string message;
        public LogType type;
        // public DateTime Timestamp { get; private set; }
        public bool isUnityLog;

        public ConsoleMessage(string message, LogType type, bool isUnityLog)
        {
            this.message = message;
            this.type = type;
            // this.Timestamp = DateTime.Now;
            this.isUnityLog = isUnityLog;
        }
    }
}
