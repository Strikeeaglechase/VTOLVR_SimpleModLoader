using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VTOLAPICommons
{
    public class InGameConsole : MonoBehaviour
    {
        private const int consoleHeight = 450;
        private const int consoleWidth = 1400;
        private const int consoleDistanceFromBottomBorder = 30;
        private const int consoleDistanceFromLeftBorder = 30;
        private const int heightPerLine = 20;
        private const int lengthPerLine = 10000;

        public bool ShowConsole { set; private get; }

        private const int consoleMessageSize = 500;

        private ConsoleMessage[] _consoleMessage;
        public ConsoleMessage[] ConsoleMessages
        { 
            private set => _consoleMessage = value; 
            get
            {
                var returnArray = new ConsoleMessage[numberOfMessages];

                if (numberOfMessages == 0) return returnArray;

                int lookIndex = nextConsoleIndex - 1;
                if (lookIndex < 0)
                    lookIndex += consoleMessageSize;

                int endIndex = nextConsoleIndex - numberOfMessages;
                if (endIndex < 0) 
                    endIndex += consoleMessageSize;

                int j = 0;
                while (true)
                {
                    returnArray[j++] = _consoleMessage[lookIndex];

                    if (lookIndex == endIndex) break;

                    if (--lookIndex < 0)
                        lookIndex += consoleMessageSize;
                }

                return returnArray;
            }
        }
        private int numberOfMessages = 0;
        private int nextConsoleIndex = 0;

        private float consoleSliderPosition = 0f;
        private bool showUnityDebugs = false;

        public void Awake()
        {
            ConsoleMessages = new ConsoleMessage[consoleMessageSize];
            Application.logMessageReceived += HandleUnityDebugLog;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10)) {
                ShowConsole = !ShowConsole;
            }
        }

        public void OnGUI()
        {
            if (!ShowConsole) return;
            int windowX = consoleDistanceFromLeftBorder;
            int windowY = Screen.height - consoleHeight - consoleDistanceFromBottomBorder;
            GUI.Window(0, new Rect(windowX, windowY, consoleWidth, consoleHeight), ConsoleCb, "SML Console (F10 to toggle)");
        }

        private void ConsoleCb(int windowId)
        {
            int windowBorders = consoleHeight - 35;
            consoleSliderPosition = GUI.VerticalSlider(new Rect(10, 25, 25, windowBorders), consoleSliderPosition, (numberOfMessages * heightPerLine) - windowBorders, 0);
            int sliderOffset = Mathf.FloorToInt(consoleSliderPosition);

            if (GUI.Button(new Rect(consoleWidth - 100, 25, 75, 20), "Clear"))
            {
                _consoleMessage = new ConsoleMessage[consoleMessageSize];
                numberOfMessages = 0;
                nextConsoleIndex = 0;
            }

            showUnityDebugs = GUI.Toggle(new Rect(consoleWidth - 120, 45, 125, 20), showUnityDebugs, "Show Unity Logs", "toggle");

            ConsoleMessage[] consoleMessages = ConsoleMessages;
            GUIStyle labelStyle = GUI.skin.GetStyle("label");
            for (int i = 0; i < consoleMessages.Length; i++)
            {
                var messageObj = consoleMessages[i];

                if (messageObj.isUnityLog && !showUnityDebugs) continue;

                switch (messageObj.type)
                {
                    case LogType.Assert:
                    case LogType.Error:
                    case LogType.Exception:
                        labelStyle.normal.textColor = Color.red;
                        break;
                    case LogType.Log:
                        labelStyle.normal.textColor = Color.white;
                        break;
                    case LogType.Warning:
                        labelStyle.normal.textColor = Color.yellow;
                        break;
                }

                GUI.Label(new Rect(35, consoleHeight - 15 - heightPerLine * (i + 1) + sliderOffset, lengthPerLine, heightPerLine), messageObj.message, labelStyle);
            }
        }

        public void HandleUnityDebugLog(string logString, string stackTrace, LogType logType)
        {
            // We don't want to delete previous logs from mods if Unity spams a bunch of stuff, so we don't add Unity stuff if we don't show it
            if (!showUnityDebugs) return;

            LogToInGameConsole(logString, logType, true);
        }

        public void LogToInGameConsole(string message, LogType type = LogType.Log, bool isUnityLog = false) 
        {
            _consoleMessage[nextConsoleIndex++] = new ConsoleMessage(message, type, isUnityLog);
            nextConsoleIndex %= consoleMessageSize;
            numberOfMessages = Math.Min(numberOfMessages + 1, consoleMessageSize);
        }
    }
}
