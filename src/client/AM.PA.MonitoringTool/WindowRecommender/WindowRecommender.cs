using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace WindowRecommender
{
    public class WindowRecommender : BaseVisualizer
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;
        private readonly ModelCore _modelCore;

        public WindowRecommender()
        {
            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            _modelEvents.MoveStarted += OnMoveStarted;

            var models = new Dictionary<IModel, int>
            {
                { new MostRecentlyActive(_modelEvents), 1}
            };
            _modelCore = new ModelCore(models);
            _modelCore.WindowsHaze += OnWindowsHaze;
        }

        private void OnWindowsHaze(object sender, IEnumerable<Rectangle> e)
        {
            _hazeOverlay.Show(e);
        }

        private void OnMoveStarted(object sender, EventArgs e)
        {
            _hazeOverlay.Hide();
        }

        public override void Start()
        {
            base.Start();
            _hazeOverlay.Start();
            _modelEvents.Start();
            _modelCore.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _hazeOverlay.Stop();
            _modelEvents.Stop();
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
