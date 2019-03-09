using Shared;
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

        public WindowRecommender()
        {
            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            _modelEvents.MoveStarted += OnMoveStarted;

            _windowStack = new WindowStack(_modelEvents);
            _windowRecorder = new WindowRecorder(_modelEvents, _windowStack);

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
            _windowRecorder.SetScores(scores, ModelCore.GetTopWindows(scores));
            _hazeOverlay.Show(GetWindowInfo(scores, _windowStack.Windows));
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
            _windowStack.Windows = NativeMethods.GetOpenWindows();
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

        internal static IEnumerable<(Rectangle rect, bool show)> GetWindowInfo(Dictionary<IntPtr, double> scores, IEnumerable<IntPtr> windowStack)
        {
            var topWindows = ModelCore.GetTopWindows(scores);
            return windowStack
                .TakeWhile(_ => topWindows.Count != 0)
                .Select(windowHandle =>
                {
                    var rect = (Rectangle)NativeMethods.GetWindowRectangle(windowHandle);
                    var contains = topWindows.Contains(windowHandle);
                    topWindows.Remove(windowHandle);
                    return (rect: rect, show: contains);
                });
        }
    }
}
