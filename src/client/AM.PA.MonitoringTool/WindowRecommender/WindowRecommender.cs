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
        }

        public override void Start()
        {
            IsRunning = true;
            _windowCache.Start();
            _modelCore.Start();
            _modelEvents.Start();

            if (TreatmentMode)
            {
                StartTreatment();
            }
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

        internal static IEnumerable<(WindowRecord windowRecord, bool show)> GetScoredWindows(Dictionary<IntPtr, double> scores, List<WindowRecord> windowStack)
        {
            if (windowStack.Count == 0)
            {
                return Enumerable.Empty<(WindowRecord windowRecord, bool show)>();
            }
            var topWindows = Utils.GetTopEntries(scores, Settings.NumberOfWindows).ToList();
            var foregroundWindow = windowStack.First();
            // If the foreground window is not one of the top scoring windows
            // remove the one with the lowest score.
            if (!topWindows.Contains(foregroundWindow.Handle))
            {
                if (topWindows.Count == Settings.NumberOfWindows)
                {
                    topWindows.RemoveAt(topWindows.Count - 1);
                }
                topWindows.Add(foregroundWindow.Handle);
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

        internal static IEnumerable<(Rectangle rectangle, bool show)> GetDrawList(IEnumerable<(WindowRecord windowRecord, bool show)> scoredWindows)
        {
            return scoredWindows.Select(scoredWindow => (WindowUtils.GetCorrectedWindowRectangle(scoredWindow.windowRecord), scoredWindow.show));
        }

        private void OnScoresChanged(object sender, Dictionary<IntPtr, double> e)
        {
            var scores = e;
            _windowRecorder.SetScores(scores, Utils.GetTopEntries(scores, Settings.NumberOfWindows));
            var scoredWindows = GetScoredWindows(scores, _windowStack.WindowRecords);
            _hazeOverlay.Show(GetDrawList(scoredWindows));
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

        private void OnMoveStarted(object sender, EventArgs e)
        {
            _hazeOverlay.Hide();
        }

        private void OnMoveEnded(object sender, EventArgs e)
        {
            var scores = _modelCore.GetScores();
            var scoredWindows = GetScoredWindows(scores, _windowStack.WindowRecords);
            _hazeOverlay.Show(GetDrawList(scoredWindows));
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _hazeOverlay.ReloadMonitors();
        }
    }
}
