using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WindowRecommender.Native;

namespace WindowRecommender
{
    public class WindowRecommender : BaseVisualizer
    {
        private readonly HazeOverlay _hazeOverlay;
        private readonly ModelEvents _modelEvents;
        private readonly Dictionary<IModel, int> _models;

        public WindowRecommender()
        {
            _hazeOverlay = new HazeOverlay();

            _modelEvents = new ModelEvents();
            //_modelEvents.WindowFocused += OnWindowFocused;
            _modelEvents.MoveStarted += OnMoveStarted;

            _models = new Dictionary<IModel, int>
            {
                { new MostRecentlyActive(_modelEvents), 1}
            };
            foreach (var model in _models.Keys)
            {
                model.OrderChanged += OnOrderChanged;
            }
        }

        private void OnOrderChanged(object sender, EventArgs e)
        {
            HazeWindows();
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

            var windows = NativeMethods.GetOpenWindows();
            foreach (var model in _models.Keys)
            {
                model.SetWindows(windows);
            }
            HazeWindows();
        }

        public override void Stop()
        {
            base.Stop();
            _hazeOverlay.Stop();
            _modelEvents.Stop();
        }

        private void HazeWindows()
        {
            var scores = new Dictionary<IntPtr, double>();
            foreach (var model in _models)
            {
                foreach (var score in model.Key.GetScores())
                {
                    if (!scores.ContainsKey(score.Key))
                    {
                        scores.Add(score.Key, 0);
                    }
                    scores[score.Key] += score.Value * model.Value;
                }
            }
            var topWindows = scores.OrderBy(x => x.Value).Select(x => x.Key).Take(Settings.NumberOfWindows);
            var windowRects = topWindows.Select(windowHandle => (Rectangle)NativeMethods.GetWindowRectangle(windowHandle));
            _hazeOverlay.Show(windowRects);
        }

        //private void OnWindowFocused(object sender, IntPtr e)
        //{
        //    var focusedWindowHandle = e;
        //    var windowTitles = NativeMethods.GetOpenWindows().Select(NativeMethods.GetWindowTitle).ToList();
        //    var focusedWindowTitle = NativeMethods.GetWindowTitle(focusedWindowHandle);

        //    var windowHandles = NativeMethods.GetOpenWindows();
        //    if (NativeMethods.IsOpenWindow(focusedWindowHandle))
        //    {
        //        if (!windowHandles.Contains(focusedWindowHandle))
        //        {
        //            Console.Out.WriteLine("inlist");
        //        }
        //    }

        //    var windowList = windowHandles.Where(windowHandle => windowHandle != focusedWindowHandle)
        //        .Take(2)
        //        .ToList();
        //    windowList.Add(focusedWindowHandle);
        //    var windowRects = windowList.Select(windowHandle =>
        //    {
        //        var rect = NativeMethods.GetWindowRectangle(windowHandle);
        //        var rectangle = new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
        //        return rectangle;
        //    });
        //    _hazeOverlay.Show(windowRects);
        //}

        //private void SetupWindows()
        //{
        //    var windowHandles = NativeMethods.GetOpenWindows();
        //    var windowTitles = windowHandles.Select(NativeMethods.GetWindowTitle).ToList();
        //    var windowRects = windowHandles.Take(3).Select(windowHandle =>
        //    {
        //        var rect = NativeMethods.GetWindowRectangle(windowHandle);
        //        var rectangle = new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
        //        return rectangle;
        //    });
        //    _hazeOverlay.Show(windowRects);
        //}

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
