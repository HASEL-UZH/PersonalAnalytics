using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class HazeOverlay
    {
        private readonly (HazeOverlayWindow window, Rectangle rectangle)[] _windows;

        internal HazeOverlay()
        {
            var monitorRects = NativeMethods.GetMonitorRects();
            _windows = monitorRects.Select(screenRect =>
            {
                var screenRectangle = (Rectangle)screenRect;
                var hazeOverlayWindow = new HazeOverlayWindow(screenRectangle);

                return (window: hazeOverlayWindow, rectangle: screenRectangle);
            }).ToArray();
        }

        public void Start()
        {
            foreach (var (window, _) in _windows)
            {
                window.Start();
            }
        }

        public void Stop()
        {
            foreach (var (window, _) in _windows)
            {
                window.Stop();
            }
        }

        internal void Show(List<(Rectangle rect, bool show)> windowInfo)
        {
            foreach (var (window, screenRectangle) in _windows)
            {
                var rectangles = Mask.Cut(screenRectangle, windowInfo);
                var transformedRectangles = rectangles.Select(rectangle =>
                    new Rectangle(rectangle.Left - screenRectangle.Left, rectangle.Top - screenRectangle.Top,
                        rectangle.Right - screenRectangle.Left, rectangle.Bottom - screenRectangle.Top));
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
    }
}
