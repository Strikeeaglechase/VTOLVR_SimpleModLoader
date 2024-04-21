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

        private string[] _consoleMessage;
        public string[] ConsoleMessages 
        { 
            private set => _consoleMessage = value; 
            get
            {
                var returnArray = new string[numberOfMessages];

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

        public void Awake()
        {
            ConsoleMessages = new string[consoleMessageSize];
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

            string[] consoleMessages = ConsoleMessages;
            for (int i = 0; i < consoleMessages.Length; i++)
            {
                GUI.Label(new Rect(35, consoleHeight - 15 - heightPerLine * (i + 1) + sliderOffset, lengthPerLine, heightPerLine), consoleMessages[i]);
            }
        }

        public void HandleUnityDebugLog(string logString, string stackTrace, LogType logType)
        {
            LogToInGameConsole(logString);
        }

        public void LogToInGameConsole(string message) 
        {
            _consoleMessage[nextConsoleIndex++] = message;
            nextConsoleIndex %= consoleMessageSize;
            numberOfMessages = Math.Min(numberOfMessages + 1, consoleMessageSize);
        }
    }
}
