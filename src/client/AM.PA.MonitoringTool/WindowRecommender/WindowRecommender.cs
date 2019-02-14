using GameOverlay.Graphics.Primitives;
using Shared;
using Shared.Helpers;
using System;
using System.Linq;
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
            _modelEvents.MoveStarted += OnMoveStarted;
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
        }

        public override void Stop()
        {
            base.Stop();
            _hazeOverlay.Stop();
            _modelEvents.Stop();
        }

        private void OnWindowFocused(object sender, string e)
        {
            var focusedWindowHandle = new IntPtr(int.Parse(e));
            var windowHandles = NativeMethods.GetOpenWindows()
                .Where(windowHandle => windowHandle != focusedWindowHandle)
                .Take(2)
                .ToList();
            windowHandles.Add(focusedWindowHandle);
            var windowRects = windowHandles.Select(windowHandle =>
            {
                var rect = NativeMethods.GetWindowRectangle(windowHandle);
                var rectangle = new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
                return rectangle;
            });
            _hazeOverlay.Show(windowRects);
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
