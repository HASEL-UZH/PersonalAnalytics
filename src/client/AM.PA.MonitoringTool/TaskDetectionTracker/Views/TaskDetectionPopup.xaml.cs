﻿// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
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

        Brush[] brushes = new Brush[]
        {
            Brushes.Beige,
            Brushes.AliceBlue
        };

        private void GenerateRectangles()
        {
            double margin = 20;
            double totalTaskBorderSpace = _tasks.Count * TaskRectangle.TaskBoundaryWidth;

            double totalDuration = _tasks.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
            double totalWidth = this.Width - (2 * margin) - totalTaskBorderSpace;
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
                    color = brushes[colors.Keys.Count % brushes.Length];
                    colors.Add(task.TaskTypeProposed, color);
                }

                var processRectangles = new ObservableCollection<ProcessRectangle>();
                double totalProcessDuration = task.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
                
                double processX = 0;
                foreach (TaskDetectionInput process in task.TimelineInfos)
                {
                    double processDuration = process.End.Subtract(process.Start).TotalSeconds;
                    double processWidth = processDuration * (width / totalProcessDuration);
                    string tooltip = string.Join(Environment.NewLine, process.WindowTitles) + Environment.NewLine + "Keystrokes: " + process.NumberOfKeystrokes + Environment.NewLine + "Mouse clicks: " + process.NumberOfMouseClicks;

                    Brush processColor;
                    bool hasProcessKey = colors.TryGetValue(process.ProcessName, out processColor);
                    if (!hasProcessKey)
                    {
                        processColor = brushes[colors.Keys.Count % brushes.Length];
                        colors.Add(process.ProcessName, processColor);
                    }

                    processRectangles.Add(new ProcessRectangle { Width = processWidth, Height = 30, X = processX, Color = processColor, Tooltip = tooltip });
                    processX += processWidth;
                }

                RectItems.Add(new TaskRectangle { X = x, Width = width, Height = 30, Color = color, ProcessRectangle = processRectangles, TaskName = task.TaskTypeProposed });
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
    }
}