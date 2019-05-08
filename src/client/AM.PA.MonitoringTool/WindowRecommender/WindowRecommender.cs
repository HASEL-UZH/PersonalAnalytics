using Microsoft.Win32;
using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WindowRecommender.Data;
using WindowRecommender.Graphics;
using WindowRecommender.Models;
using WindowRecommender.Util;

namespace WindowRecommender
{
    public class WindowRecommender : BaseTracker
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;
        private readonly ModelCore _modelCore;
        private readonly WindowRecorder _windowRecorder;
        private readonly WindowStack _windowStack;
        private readonly WindowCache _windowCache;

        public WindowRecommender()
        {
            Name = "Window Recommender";

            Settings.Enabled = Queries.GetEnabledSetting();
            Settings.NumberOfWindows = Queries.GetNumberOfWindowsSetting();
            Settings.TreatmentMode = Queries.GetTreatmentModeSetting();

            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();

            _windowCache = new WindowCache(_modelEvents);
            _windowStack = new WindowStack(_windowCache);
            _windowRecorder = new WindowRecorder(_windowCache, _windowStack);

            _modelCore = new ModelCore(new (IModel model, double weight)[]
            {
                (new MostRecentlyActive(_windowCache), 1),
                (new Frequency(_windowCache), 1),
                (new Duration(_windowCache), 1),
                (new TitleSimilarity(_windowCache), 1),
            });
            _modelCore.ScoreChanged += OnScoresChanged;
            _modelCore.WindowsChanged += OnWindowsChanged;
        }

        public override void Start()
        {
            IsRunning = true;
            if (TreatmentMode)
            {
                StartTreatment();
            }
            _windowCache.Start();
            _modelCore.Start();
            _modelEvents.Start();

        }

        public override void Stop()
        {
            IsRunning = false;
            _modelEvents.Stop();
            if (TreatmentMode)
            {
                StopTreatment();
            }
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            // During development, drop and recreate tables
            Queries.DropTables();
            Queries.CreateTables();
        }

        public override void UpdateDatabaseTables(int version)
        {
            // During development, drop and recreate tables
            Queries.DropTables();
            Queries.CreateTables();
        }

        public override string GetVersion()
        {
            return VersionHelper.GetFormattedVersion(new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version);
        }

        public override bool IsEnabled()
        {
            return WindowRecommenderEnabled;
        }

        public bool WindowRecommenderEnabled
        {
            private get => Settings.Enabled;
            set
            {
                var enabled = value;
                if (enabled != Settings.Enabled)
                {
                    Settings.Enabled = enabled;
                    Queries.SetEnabledSetting(enabled);

                    // start/stop tracker if necessary
                    if (!enabled && IsRunning)
                    {
                        Stop();
                    }
                    else if (enabled && !IsRunning)
                    {
                        CreateDatabaseTablesIfNotExist();
                        Start();
                    }
                }
            }
        }

        public int NumberOfWindows
        {
            get => Settings.NumberOfWindows;
            set
            {
                var numberOfWindows = value;
                if (numberOfWindows != Settings.NumberOfWindows)
                {
                    Settings.NumberOfWindows = numberOfWindows;
                    Queries.SetNumberOfWindowsSetting(numberOfWindows);
                }
            }
        }

        public bool TreatmentMode
        {
            get => Settings.TreatmentMode;
            set
            {
                var treatmentMode = value;
                if (treatmentMode != Settings.TreatmentMode)
                {
                    Settings.TreatmentMode = treatmentMode;
                    Queries.SetTreatmentModeSetting(treatmentMode);

                    if (treatmentMode)
                    {
                        StartTreatment();
                    }
                    else
                    {
                        StopTreatment();
                    }
                }
            }
        }

        internal static IEnumerable<(WindowRecord windowRecord, bool show)> GetScoredWindows(List<IntPtr> topWindows, List<WindowRecord> windowStack)
        {
            if (windowStack.Count == 0)
            {
                return Enumerable.Empty<(WindowRecord windowRecord, bool show)>();
            }
            var foregroundWindow = windowStack.First();
            // If the foreground window is not one of the top scoring windows
            // add it as first element and
            // remove the one with the lowest score if necessary.
            if (!topWindows.Contains(foregroundWindow.Handle))
            {
                if (topWindows.Count == Settings.NumberOfWindows)
                {
                    topWindows.RemoveAt(topWindows.Count - 1);
                }
                topWindows.Insert(0, foregroundWindow.Handle);
            }
            return windowStack
                .TakeWhile(_ => topWindows.Count != 0)
                .Select(windowRecord =>
                {
                    var show = topWindows.Contains(windowRecord.Handle);
                    topWindows.Remove(windowRecord.Handle);
                    return (windowRecord, show);
                });
        }

        private void OnScoresChanged(object sender, Dictionary<IntPtr, double> e)
        {
            var scores = e;
            _windowRecorder.SetScores(scores, Utils.GetTopEntries(scores, Settings.NumberOfWindows));
            Queries.SaveScoreChange(scores.Select(pair => new ScoreRecord(pair.Key, sender.GetType().Name, pair.Value)));
        }

        private void OnWindowsChanged(object sender, List<IntPtr> e)
        {
            var topWindows = e;
            UpdateDrawing(topWindows);
        }

        private void StartTreatment()
        {
            _modelEvents.MoveStarted += OnMoveStarted;
            _modelEvents.MoveEnded += OnMoveEnded;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            _hazeOverlay.Start();
        }

        private void StopTreatment()
        {
            _modelEvents.MoveStarted -= OnMoveStarted;
            _modelEvents.MoveEnded -= OnMoveEnded;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _hazeOverlay.Stop();
        }

        private void UpdateDrawing(List<IntPtr> topWindows)
        {
            var scoredWindows = GetScoredWindows(topWindows, _windowStack.WindowRecords).ToList();
            var scoredWindowRectangles = scoredWindows
                .Select(tuple => (tuple.windowRecord, rectangle: WindowUtils.GetCorrectedWindowRectangle(tuple.windowRecord), tuple.show))
                .ToList();
            _hazeOverlay.Show(scoredWindowRectangles.Select(tuple => (tuple.rectangle, tuple.show)));

            var desktopWindowRecords = scoredWindowRectangles
                .Concat(_windowStack.WindowRecords
                    .Skip(scoredWindows.Count)
                    .Select(windowRecord => (windowRecord, rectangle: WindowUtils.GetCorrectedWindowRectangle(windowRecord), show: false))
                )
                .Select((tuple, i) => new DesktopWindowRecord(tuple.windowRecord.Handle, !tuple.show, i, tuple.rectangle));
            Queries.SaveDesktopEvents(desktopWindowRecords);
        }

        private void OnMoveStarted(object sender, EventArgs e)
        {
            _hazeOverlay.Hide();
        }

        private void OnMoveEnded(object sender, EventArgs e)
        {
            var topWindows = _modelCore.GetTopWindows();
            UpdateDrawing(topWindows);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _hazeOverlay.ReloadMonitors();
        }
    }
}
