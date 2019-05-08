using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Data;
using WindowRecommender.Native;

namespace WindowRecommender.Graphics
{
    internal class HazeOverlay
    {
        private (HazeOverlayWindow window, Rectangle rectangle)[] _windows;
        private bool _isRunning;

        internal HazeOverlay()
        {
            _windows = new (HazeOverlayWindow window, Rectangle rectangle)[0];
            _isRunning = false;
        }

        public void Start()
        {
            CreateMonitorWindows();
            _isRunning = true;
            foreach (var (window, _) in _windows)
            {
                window.Start();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var (window, _) in _windows)
            {
                window.Stop();
            }
        }

        internal void Show(IEnumerable<(Rectangle rectangle, bool show)> windowInfo)
        {
            var windowList = windowInfo.ToList();
            foreach (var (window, screenRectangle) in _windows)
            {
                var rectangles = Mask.Cut(screenRectangle, windowList);
                var transformedRectangles = rectangles.Select(rectangle => Rectangle.TranslatedRelative(screenRectangle, rectangle));
                window.Show(transformedRectangles);
            }
        }

        internal void Hide()
        {
            foreach (var (window, _) in _windows)
            {
                window.Hide();
            }
        }

        internal void ReloadMonitors()
        {
            if (_isRunning)
            {
                foreach (var (window, _) in _windows)
                {
                    window.Stop();
                    window.Dispose();
                }
            }

            CreateMonitorWindows();

            if (_isRunning)
            {
                foreach (var (window, _) in _windows)
                {
                    window.Start();
                }
            }
        }

        private void CreateMonitorWindows()
        {
            var monitorRects = NativeMethods.GetMonitorWorkingAreas().Select(rect => (Rectangle)rect).ToList();
            Queries.SaveScreenEvents(monitorRects);
            _windows = monitorRects.Select(screenRect =>
            {
                var screenRectangle = screenRect;
                var hazeOverlayWindow = new HazeOverlayWindow(screenRectangle);

                return (window: hazeOverlayWindow, rectangle: screenRectangle);
            }).ToArray();
        }
    }
}
