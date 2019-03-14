using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender
{
    internal class HazeOverlayWindow
    {
        private readonly GraphicsWindow _window;

        private SolidBrush _brush;
        private Rectangle[] _rectangles;

        internal HazeOverlayWindow(Rectangle screenRectangle)
        {
            _rectangles = new Rectangle[0];

            _window = new GraphicsWindow
            {
                IsTopmost = true,
                FPS = Settings.FramesPerSecond,
                X = screenRectangle.Left,
                Y = screenRectangle.Top,
                Width = screenRectangle.Right - screenRectangle.Left,
                Height = screenRectangle.Bottom - screenRectangle.Top
            };
            _window.SetupGraphics += OnSetupGraphics;
            _window.DrawGraphics += OnDrawGraphics;
            _window.DestroyGraphics += OnDestroyGraphics;
        }

        ~HazeOverlayWindow()
        {
            _window.Dispose();
        }

        public void Start()
        {
            _window.IsVisible = true;
            _window.StartThread();
        }

        public void Stop()
        {
            _rectangles = new Rectangle[0];
            _window.IsVisible = false;
            _window.StopThread();
        }

        internal void Show(IEnumerable<Rectangle> rectangles)
        {
            _rectangles = rectangles.ToArray();
        }

        internal void Hide()
        {
            _rectangles = new Rectangle[0];
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
            foreach (var rectangle in _rectangles)
            {
                graphics.FillRectangle(_brush, rectangle);
            }
        }

        private void OnDestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            _brush.Dispose();
        }
    }
}
