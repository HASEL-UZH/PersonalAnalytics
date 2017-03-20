// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using TaskDetectionTracker.Model;
using System.Linq;
using System.Windows.Media;
using System;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Shapes;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window
    {
        public bool ValidationComplete { get; set; } // TODO: remove

        private DispatcherTimer _popUpReminderTimer;

        private List<TaskDetection> _tasks;
        private Dictionary<string, Brush> colors = new Dictionary<string, Brush>();

        public ObservableCollection<TaskRectangle> RectItems { get; set; }
         
        //Canvas width
        public static double CanvasWidth { get { return 3000; } }

        public TaskDetectionPopup(List<TaskDetection> tasks)
        {
            InitializeComponent();

            this.StateChanged += Window_StateChanged;
            this.Closing += Window_OnClosing;
            this.SizeChanged += Window_SizeChanged;

            Timeline.DataContext = this;
            
            this._tasks = tasks;
            StartTime.Inlines.Add(_tasks.First().Start.ToShortTimeString());
            EndTime.Inlines.Add(_tasks.Last().End.ToShortTimeString());
            
            RectItems = new ObservableCollection<TaskRectangle>();
            GenerateRectangles();
        }

        #region Handle PopUp Response (Reminder, avoid closing before validated, etc.)

        /// <summary>
        /// Prevent the window from closing. Minimize it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_OnClosing(object sender, CancelEventArgs e)
        {
            // TODO: add messagebox in red to emphasize how important it is to answer this popup. And say please and thanks ;)
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Start the popup reminder when the user minimizes the popup
        /// Stop the popup reminder when the user opens/maximizes the popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch(WindowState)
            {
                case WindowState.Minimized:
                    StartReminderTimer();
                    break;
                case WindowState.Maximized:
                case WindowState.Normal:
                    StopReminderTimer();
                    RedrawTimeline();
                    break;
            }
        }

        private void StartReminderTimer()
        {
            if (_popUpReminderTimer != null)
            {
                _popUpReminderTimer = null;
            }

            _popUpReminderTimer = new DispatcherTimer();
            _popUpReminderTimer.Interval = Settings.PopUpReminderInterval;
            _popUpReminderTimer.Tick += PopUpReminder_Tick;
            _popUpReminderTimer.Start();
        }

        private void StopReminderTimer()
        {
            if (_popUpReminderTimer != null)
            {
                _popUpReminderTimer.Stop();
                _popUpReminderTimer = null;
            }
        }

        private void PopUpReminder_Tick(object sender, EventArgs e)
        {
            // TODO: add text in red to emphasize how important it is to answer this popup. And say please and thanks ;)
            WindowState = WindowState.Normal;
        }

        #endregion

        Brush[] taskBrushes = new Brush[]
        {
            Brushes.Beige,
            Brushes.AliceBlue
        };

        Brush[] processBrushes = new Brush[]
        {
            Brushes.Violet,
            Brushes.Green
        };

        private void GenerateRectangles()
        {
            double margin = 20;
            double totalTaskBorderSpace = _tasks.Count * TaskRectangle.TaskBoundaryWidth;

            double totalDuration = _tasks.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
            double totalWidth = CanvasWidth - (2 * margin) - totalTaskBorderSpace;
            double x = margin;

            for (int i = 0; i < _tasks.Count; i++)
            {
                TaskDetection task = _tasks.ElementAt(i);
                double duration = task.End.Subtract(task.Start).TotalSeconds;
                double width = duration * (totalWidth / totalDuration);
            
                Brush color;

                bool hasKey = colors.TryGetValue(task.TaskTypeProposed, out color);
                if (!hasKey)
                {
                    color = taskBrushes[colors.Keys.Count % taskBrushes.Length];
                    colors.Add(task.TaskTypeProposed, color);
                }

                var processRectangles = new ObservableCollection<ProcessRectangle>();
                double totalProcessDuration = task.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
                double processBorderWidth = (task.TimelineInfos.Count - 1) * ProcessRectangle.TaskBoundaryWidth;

                double processX = 0;
                var lastProcess = task.TimelineInfos.Last();
                foreach (TaskDetectionInput process in task.TimelineInfos)
                {
                    double processDuration = process.End.Subtract(process.Start).TotalSeconds;
                    
                    double processWidth = processDuration * ((width - processBorderWidth) / totalProcessDuration);
                    string tooltip = string.Join(Environment.NewLine, process.WindowTitles) + Environment.NewLine + "Keystrokes: " + process.NumberOfKeystrokes + Environment.NewLine + "Mouse clicks: " + process.NumberOfMouseClicks;

                    Brush processColor;
                    bool hasProcessKey = colors.TryGetValue(process.ProcessName, out processColor);
                    if (!hasProcessKey)
                    {
                        processColor = processBrushes[colors.Keys.Count % processBrushes.Length];
                        colors.Add(process.ProcessName, processColor);
                    }

                    bool visibility = lastProcess.Equals(process) ? false : true;
                    processRectangles.Add(new ProcessRectangle { Data = process, Width = processWidth, Height = 30, X = processX, Color = processColor, Tooltip = tooltip, IsVisible = visibility });
                    processX += (processWidth + ProcessRectangle.TaskBoundaryWidth);
                }

                RectItems.Add(new TaskRectangle { X = x, Width = width, Height = 30, Color = color, ProcessRectangle = processRectangles, TaskName = task.TaskTypeProposed, Timestamp = task.End.ToShortTimeString() });
                x += (width + TaskRectangle.TaskBoundaryWidth);
            }
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationComplete = true; //TODO: validate input & only enable save-button when complete
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawTimeline();
        }

        private void RedrawTimeline()
        {
            RectItems.Clear();
            GenerateRectangles();
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var processToMerge = ((sender as Rectangle).DataContext as ProcessRectangle).Data;
            
            foreach (TaskDetection task in _tasks)
            {
                int index = task.TimelineInfos.FindIndex(p => p.Equals(processToMerge));
                if (index != -1 && (index + 1) < task.TimelineInfos.Count)
                {
                    SplitProcesses(processToMerge, task.TimelineInfos[++index]);
                }
            }
        }

        private void SplitProcesses(TaskDetectionInput process1, TaskDetectionInput process2)
        {
            //TODO: merge process 1 and process 2
        }
    }
}
