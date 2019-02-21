using GameOverlay.Graphics;
using GameOverlay.Graphics.Primitives;
using GameOverlay.Utilities;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class HazeOverlay : IDisposable
    {
        private const int FramesPerSecond = 60;

        private readonly OverlayWindow _window;
        private readonly D2DDevice _device;
        private readonly FrameTimer _frameTimer;
        private readonly D2DSolidColorBrush _brush;
        private readonly Rectangle _screenRectangle;

        private bool _shouldDraw;
        private List<Rectangle> _rectangles;

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
            _screenRectangle = new Rectangle(0, 0, monitorDimensions.Right, monitorDimensions.Bottom);

            _device = new D2DDevice(new DeviceOptions
            {
                AntiAliasing = true,
                Hwnd = _window.WindowHandle,
                MeasureFps = false,
                MultiThreaded = false,
                VSync = false
            });

            _brush = _device.CreateSolidColorBrush(0, 0, 0, Settings.OverlayAlpha);

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
            _rectangles = Mask.Cut(_screenRectangle, rectangle);
            _shouldDraw = true;
        }

        internal void Show(IEnumerable<Rectangle> rectangles)
        {
            _rectangles = Mask.Cut(_screenRectangle, rectangles);
            _shouldDraw = true;
        }

        internal void Hide()
        {
            _shouldDraw = false;
        }

        private void OnFrame(FrameTimer timer, D2DDevice device)
        {
            device.ClearScene();
            if (_shouldDraw)
            {
                foreach (var rectangle in _rectangles)
                {
                    device.FillRectangle(rectangle, _brush);
                }
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
