using Microsoft.Win32;
using Shared;
using Shared.Data;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WindowRecommender.Data;
using WindowRecommender.Models;
using WindowRecommender.Native;

namespace WindowRecommender
{
    public class WindowRecommender : BaseTracker
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;
        private readonly ModelCore _modelCore;
        private readonly WindowRecorder _windowRecorder;
        private readonly WindowStack _windowStack;

        private bool _enabled;

        public WindowRecommender()
        {
            Name = "Window Recommender";

            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            _modelEvents.MoveStarted += OnMoveStarted;
            _modelEvents.MoveEnded += OnMoveEnded;

            _windowStack = new WindowStack(_modelEvents);
            _windowRecorder = new WindowRecorder(_modelEvents, _windowStack);

            _modelCore = new ModelCore(new (IModel model, double weight)[]
            {
                (new MostRecentlyActive(_modelEvents), 1),
                (new Frequency(_modelEvents), 1),
                (new Duration(_modelEvents), 1),
            });
            _modelCore.ScoreChanged += OnScoresChanged;

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        ~WindowRecommender()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }

        public override void Start()
        {
            IsRunning = true;
            _windowStack.Windows = NativeMethods.GetOpenWindows();
            _hazeOverlay.Start();
            _modelCore.Start();
            _modelEvents.Start();
        }

        public override void Stop()
        {
            IsRunning = false;
            _hazeOverlay.Stop();
            _modelEvents.Stop();
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Queries.CreateTables();
        }

        public override void UpdateDatabaseTables(int version)
        {
            // No update in first version
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
            private get
            {
                _enabled = Database.GetInstance().GetSettingsBool(Settings.EnabledSettingDatabaseKey, Settings.IsEnabledByDefault);
                return _enabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _enabled) return;

                // update settings
                Database.GetInstance().SetSettings(Settings.EnabledSettingDatabaseKey, value);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    CreateDatabaseTablesIfNotExist();
                    Start();
                }

                Database.GetInstance().LogInfo($"The participant updated the setting '{Settings.EnabledSettingDatabaseKey}' to {updatedIsEnabled}");
            }
        }

        internal static IEnumerable<(Rectangle rect, bool show)> GetDrawList(Dictionary<IntPtr, double> scores, IEnumerable<IntPtr> windowStack)
        {
            if (windowStack.Count() == 0)
            {
                return Enumerable.Empty<(Rectangle rect, bool show)>();
            }
            var topWindows = Utils.GetTopEntries(scores, Settings.NumberOfWindows).ToList();
            var foregroundWindow = windowStack.First();
            // If the foreground window is not one of the top scoring windows
            // remove the one with the lowest score.
            if (!topWindows.Contains(foregroundWindow))
            {
                topWindows.RemoveAt(topWindows.Count - 1);
            }
            return windowStack
                .TakeWhile(_ => topWindows.Count != 0)
                .Select(windowHandle =>
                {
                    var rect = (Rectangle)NativeMethods.GetWindowRectangle(windowHandle);
                    var contains = topWindows.Contains(windowHandle) || windowHandle == foregroundWindow;
                    topWindows.Remove(windowHandle);
                    return (rect: rect, show: contains);
                });
        }

        private void OnScoresChanged(object sender, Dictionary<IntPtr, double> e)
        {
            var scores = e;
            _windowRecorder.SetScores(scores, Utils.GetTopEntries(scores, Settings.NumberOfWindows));
            _hazeOverlay.Show(GetDrawList(scores, _windowStack.Windows));
        }

        private void OnMoveStarted(object sender, EventArgs e)
        {
            _hazeOverlay.Hide();
        }

        private void OnMoveEnded(object sender, EventArgs e)
        {
            _hazeOverlay.Show(GetDrawList(_modelCore.GetScores(), _windowStack.Windows));
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _hazeOverlay.ReloadMonitors();
        }
    }
}
