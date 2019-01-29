using GameOverlay.Graphics.Primitives;
using Shared;
using Shared.Helpers;
using System;
using System.Reflection;
using WindowRecommender.Native;

namespace WindowRecommender
{
    public class WindowRecommender : BaseVisualizer
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;

        public WindowRecommender()
        {
            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            _modelEvents.WindowFocused += OnWindowFocused;
        }

        public override void Start()
        {
            base.Start();
            _hazeOverlay.Start();
            _modelEvents.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _hazeOverlay.Stop();
            _modelEvents.Stop();
        }

        private void OnWindowFocused(object sender, string e)
        {
            var windowHandle = new IntPtr(int.Parse(e));
            var rect = NativeMethods.GetWindowRect(windowHandle);
            var rectangle = new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
            _hazeOverlay.Show(rectangle);
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
