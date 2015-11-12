// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using OcrLibrary.Helpers;

namespace OcrLibrary.Models
{
    public class ContextEntry
    {
        public ContextEntry()
        {
            Timestamp = DateTime.MinValue;
            OcrText = string.Empty;
            Confidence = 0.0;
            WindowName = string.Empty;
            ProcessName = string.Empty;
            Screenshot = null;
        }

        public ContextEntry(DateTime ts, string window, string process)
        {
            Timestamp = ts;
            WindowName = window;
            ProcessName = process;
        }

        public DateTime Timestamp { get; set; }
        public string OcrText { get; set; }
        public double Confidence { get; set; }
        public string WindowName { get; set; }
        public string ProcessName { get; set; }
        public Screenshot Screenshot { get; set; }
    }
}
