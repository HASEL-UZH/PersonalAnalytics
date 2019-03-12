using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Collections.Generic;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class HazeOverlay
    {
        private readonly GraphicsWindow _window;
        private readonly Rectangle _screenRectangle;
        private SolidBrush _brush;

        private bool _shouldDraw;
        private List<Rectangle> _rectangles;

        internal HazeOverlay()
        {
            var monitorDimensions = NativeMethods.GetPrimaryMonitorDimensions();
            _screenRectangle = new Rectangle(0, 0, monitorDimensions.Right, monitorDimensions.Bottom);

            var graphics = new Graphics
            {
                MeasureFPS = true
            };

            _window = new GraphicsWindow(graphics)
            {
                IsTopmost = true,
                IsVisible = true,
                FPS = Settings.FramesPerSecond,
                X = 0,
                Y = 0,
                Width = monitorDimensions.Right - monitorDimensions.Left,
                Height = monitorDimensions.Bottom - monitorDimensions.Top
            };

            _window.SetupGraphics += OnSetupGraphics;
            _window.DrawGraphics += OnDrawGraphics;
            _window.DestroyGraphics += OnDestroyGraphics;
        }

        ~HazeOverlay()
        {
            _window.Dispose();
        }

        public void Start()
        {
            _window.StartThread();
        }

        public void Stop()
        {
            _shouldDraw = false;
            _window.StopThread();
        }

        internal void Show(IEnumerable<(Rectangle rect, bool show)> windowInfo)
        {
            _rectangles = Mask.Cut(_screenRectangle, windowInfo);
            _shouldDraw = true;
        }

        internal void Hide()
        {
            _shouldDraw = false;
        }

        private void OnSetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var graphics = e.Graphics;
            _brush = graphics.CreateSolidBrush(0, 0, 0, Settings.OverlayAlpha);
        }

        private void OnDrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.ClearScene();
            if (_shouldDraw)
            {
                foreach (var rectangle in _rectangles)
                {
                    graphics.FillRectangle(_brush, rectangle);
                }
            }
        }

        private void OnDestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            _brush.Dispose();
        }
    }
}
