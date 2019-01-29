using GameOverlay.Graphics;
using GameOverlay.Graphics.Primitives;
using GameOverlay.Utilities;
using GameOverlay.Windows;
using System;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class HazeOverlay : IDisposable
    {
        private const int FramesPerSecond = 60;
        private const int ColorAlpha = 64;

        private readonly OverlayWindow _window;
        private readonly D2DDevice _device;
        private readonly FrameTimer _frameTimer;
        private readonly D2DSolidColorBrush _brush;

        private bool _shouldDraw;
        private Rectangle _rectangle;

        internal HazeOverlay()
        {
            var monitorDimensions = NativeMethods.GetPrimaryMonitorDimensions();

            _window = new OverlayWindow(new OverlayOptions
            {
                BypassTopmost = false,
                Height = monitorDimensions.Bottom - monitorDimensions.Top,
                Width = monitorDimensions.Right - monitorDimensions.Left,
                WindowTitle = "HazeOverlay",
                X = 0,
                Y = 0
            });

            _device = new D2DDevice(new DeviceOptions
            {
                AntiAliasing = true,
                Hwnd = _window.WindowHandle,
                MeasureFps = false,
                MultiThreaded = false,
                VSync = false
            });

            _brush = _device.CreateSolidColorBrush(0, 0, 0, ColorAlpha);

            _frameTimer = new FrameTimer(_device, FramesPerSecond);
            _frameTimer.OnFrame += OnFrame;
        }

        public void Start()
        {
            _frameTimer.Start();
        }

        public void Stop()
        {
            _shouldDraw = false;
            _frameTimer.Stop();
        }

        internal void Show(Rectangle rectangle)
        {
            _rectangle = rectangle;
            _shouldDraw = true;
        }

        private void OnFrame(FrameTimer timer, D2DDevice device)
        {
            device.ClearScene();
            if (_shouldDraw)
            {
                device.FillRectangle(new Rectangle(0, 0, _rectangle.Left, _rectangle.Bottom), _brush);
                device.FillRectangle(new Rectangle(_rectangle.Left, 0, _window.Width, _rectangle.Top), _brush);
                device.FillRectangle(new Rectangle(_rectangle.Right, _rectangle.Top, _window.Width, _window.Height), _brush);
                device.FillRectangle(new Rectangle(0, _rectangle.Bottom, _rectangle.Right, _window.Height), _brush);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _window.Dispose();
                _device.Dispose();
                _frameTimer.Dispose();
                _brush.Dispose();
            }
        }
    }
}
