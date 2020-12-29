// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;

namespace BluetoothLowEnergy
{

    public delegate void OnNewConsoleLogEvent(string message);

    public delegate void OnNewLogFileLogEvent(Exception message);
    
    public class LoggerWrapper
    {
        private static LoggerWrapper _instance;

        private LoggerWrapper() { }

        public static LoggerWrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoggerWrapper();
                }
                return _instance;
            }
        }

        public event OnNewConsoleLogEvent NewConsoleMessage;

        public event OnNewLogFileLogEvent NewLogFileMessage;

        internal void WriteToConsole(string message)
        {
            NewConsoleMessage?.Invoke(message);
        }

        internal void WriteToLogFile(Exception message)
        {
            NewLogFileMessage?.Invoke(message);
        }

    }
}