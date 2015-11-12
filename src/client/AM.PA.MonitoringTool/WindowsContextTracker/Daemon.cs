// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.Windows.Threading;
using WindowsContextTracker.Helpers;
using OcrLibrary.Helpers;
using OcrLibrary.Models;
using Shared;
using Shared.Data;

namespace WindowsContextTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        #region FIELDS

        private DispatcherTimer _screenshotterTimer;
        private ScreenshotChangedTracker _screenshotChangedTracker;

        #endregion

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "Windows Context Tracker";
        }

        public override void Start()
        {
            _screenshotChangedTracker = new ScreenshotChangedTracker();
            _screenshotterTimer = new DispatcherTimer();
            _screenshotterTimer.Interval = Settings.WindowScreenshotInterval;
            _screenshotterTimer.Tick += RunContextRecognition;
            _screenshotterTimer.Start();

            IsRunning = true;
        }

        public override void Stop()
        {
            if (_screenshotterTimer != null)
            {
                _screenshotterTimer.Stop();
                _screenshotterTimer = null;
            }

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, ocrText TEXT, confidentality NUMBER, windowName TEXT, processName TEXT)");
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabled;
        }

        #endregion

        #region Daemon

        /// <summary>
        /// Every x seconds the Context Recognition algorithm is run if the window has changed.
        /// Some info about the context is added (currently: process + windows information)
        /// Then, the background worker is started for OCR recognition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunContextRecognition(object sender, EventArgs e)
        {
            // determine whether store or not
            if (_screenshotChangedTracker == null || !_screenshotChangedTracker.WindowChanged()) return;

            // capture the screenshot
            var sc = ScreenCapture.CaptureActiveWindowHq();
            if (sc == null || sc.Image == null) return; // screenshot capture unsuccessful

            // get window & process names (additional contextual information)
            var currentWindowName = _screenshotChangedTracker.GetWindowText();
            var currentProcessName = _screenshotChangedTracker.GetProcessName();

            var ce = new ContextEntry
            {
                Timestamp = DateTime.Now,
                WindowName = currentWindowName,
                ProcessName = currentProcessName,
                Confidence = OcrLibrary.Settings.OcrConfidenceAcceptanceThreshold, // so it won't be saved if it isn't overwritten
                OcrText = string.Empty,
                Screenshot = sc
            };

            // runs the OCR process and get's the result as a context entry
            ce = OcrEngine.GetInstance().RunOcr(ce);

            // only save in database if useful accuracy & enough content
            if (ce != null &&
                ce.Confidence < OcrLibrary.Settings.OcrConfidenceAcceptanceThreshold &&
                ce.OcrText.Length > OcrLibrary.Settings.OcrTextLengthAcceptanceThreshold)
            {
                SaveContextEntryToDatabase(ce);
            }

            // dispose data (to free up RAM)
            DisposeData(ce);
        }

        /// <summary>
        /// try to give resources free
        /// TODO: most likely, this method is not used
        /// </summary>
        /// <param name="ce"></param>
        private static void DisposeData(ContextEntry ce)
        {
            if (ce == null || ce.Screenshot == null) return;
            if (ce.Screenshot.Image != null) ce.Screenshot.Image.Dispose();
            if (ce.Screenshot != null) ce.Screenshot.Dispose();
            ce = null;
        }

        private static void SaveContextEntryToDatabase(ContextEntry contextEntry)
        {
            if (contextEntry == null ||
                contextEntry.OcrText == string.Empty || // no text recognized
                contextEntry.Confidence < 0.5) // makes no sense to save if recognition is too in-accurate
                return;

            var query = "INSERT INTO " + Settings.DbTable +
                        " (time, timestamp, ocrText, confidentality, windowName, processName) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                        Database.GetInstance().QTime(contextEntry.Timestamp) + ", " +
                        Database.GetInstance().Q(contextEntry.OcrText) + ", " +
                        Database.GetInstance().Q(contextEntry.Confidence.ToString()) + ", " +
                        Database.GetInstance().Q(contextEntry.WindowName) + ", " +
                        Database.GetInstance().Q(contextEntry.ProcessName) + ");";

            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        #endregion

        #endregion
    }
}
