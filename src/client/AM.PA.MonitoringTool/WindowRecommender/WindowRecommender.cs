using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WindowRecommender.Data;
using WindowRecommender.Native;

namespace WindowRecommender
{
    public class WindowRecommender : BaseTracker
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;
        private readonly ModelCore _modelCore;
        private readonly WindowRecorder _windowRecorder;

        public WindowRecommender()
        {
            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            _modelEvents.MoveStarted += OnMoveStarted;

            _windowRecorder = new WindowRecorder(_modelEvents);

            var models = new Dictionary<IModel, int>
            {
                { new MostRecentlyActive(_modelEvents), 1}
            };
            _modelCore = new ModelCore(models);
            _modelCore.ScoreChanged += OnScoresChanged;
        }

        private void OnScoresChanged(object sender, Dictionary<IntPtr, double> e)
        {
            var scores = e;
            var topWindows = scores.OrderByDescending(x => x.Value).Select(x => x.Key).Take(Settings.NumberOfWindows).ToList();
            var windowRects = topWindows.Select(windowHandle => (Rectangle)NativeMethods.GetWindowRectangle(windowHandle));
            _windowRecorder.SetScores(scores, topWindows);
            _hazeOverlay.Show(windowRects);
        }

        private void OnMoveStarted(object sender, EventArgs e)
        {
            _hazeOverlay.Hide();
        }

        public override void Start()
        {
            IsRunning = true;
            _hazeOverlay.Start();
            _modelEvents.Start();
            _modelCore.Start();
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
            return true;
        }
    }
}
