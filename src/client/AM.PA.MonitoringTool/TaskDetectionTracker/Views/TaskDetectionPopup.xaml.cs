// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using TaskDetectionTracker.Model;
using System.Linq;
using System;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Diagnostics;
using TaskDetectionTracker.Views.Converters;
using System.Windows.Media;
using Shared.Helpers;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window, INotifyPropertyChanged
    {
        #region Validation of save button
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _validationComplete = false;
        public bool ValidationComplete { get { return _validationComplete; } set { _validationComplete = value; OnPropertyChanged("ValidationComplete"); } }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        private DispatcherTimer _popUpReminderTimer;
        private List<TaskDetection> _tasks;
        public ObservableCollection<TaskRectangle> RectItems { get; set; }
        public static double CanvasWidth { get; set; }

        /// <summary>
        /// Create a new Popup with the tasks in the parameter
        /// </summary>
        /// <param name="tasks"></param>
        public TaskDetectionPopup(List<TaskDetection> tasks)
        {
            InitializeComponent();

            //Event handlers
            this.StateChanged += Window_StateChanged;
            this.Closing += Window_OnClosing;
            this.SizeChanged += Window_SizeChanged;

            //Set task context
            Timeline.DataContext = this;
            Save.DataContext = this;
            
            //Create timeline
            this._tasks = tasks;
            WindowTitleBar.Text = WindowTitleBar.Text 
                + " (from " + _tasks.First().Start.ToShortTimeString() + " to " + _tasks.Last().End.ToShortTimeString() + ")";
            //StartTime.Inlines.Add(_tasks.First().Start.ToShortTimeString());
            //EndTime.Inlines.Add(_tasks.Last().End.ToShortTimeString());

            double minDuration = _tasks.Min(t => t.TimelineInfos.Min(p => p.End.Subtract(p.Start))).TotalSeconds;
            double totalDuration = _tasks.Sum(t => t.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds));
            double timeLineWidth = 1500; // TODO: change back totalDuration / minDuration * Settings.MinimumProcessWidth;
            CanvasWidth = Math.Max(timeLineWidth, Settings.MinimumTimeLineWidth);
            
            RectItems = new ObservableCollection<TaskRectangle>();
            this.Loaded += TaskDetectionPopup_Loaded;
            GenerateRectangles();
        }

        private void TaskDetectionPopup_Loaded(object sender, RoutedEventArgs e)
        {
            DrawLegend();
        }

        #region Handle PopUp Response (Reminder, avoid closing before validated, etc.)

        /// <summary>
        /// Prevent the window from closing. Minimize it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_OnClosing(object sender, CancelEventArgs e)
        {
            if (DialogResult != true || ValidationComplete == false)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
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
            BegForParticipation.Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
        }

        #endregion

        #region Draw Timeline

        /// <summary>
        /// Draws the timeline
        /// </summary>
        private void GenerateRectangles()
        {
            //margin on the left and right side of the timeline
            double margin = 20;
            double totalTaskBorderSpace = _tasks.Count * TaskRectangle.TaskBoundaryWidth;

            double totalDuration = _tasks.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
            double totalWidth = CanvasWidth - (2 * margin) - totalTaskBorderSpace;
            double x = margin;

            //draw each task
            for (int i = 0; i < _tasks.Count; i++)
            {
                TaskDetection task = _tasks.ElementAt(i);
                double duration = task.End.Subtract(task.Start).TotalSeconds;
                double width = duration * (totalWidth / totalDuration);
            
                var processRectangles = new ObservableCollection<ProcessRectangle>();
                double totalProcessDuration = task.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
                double processBorderWidth = (task.TimelineInfos.Count - 1) * ProcessRectangle.TaskBoundaryWidth;

                double processX = 0;
                var lastProcess = task.TimelineInfos.Last();

                //draw each process
                foreach (TaskDetectionInput process in task.TimelineInfos)
                {
                    double processDuration = process.End.Subtract(process.Start).TotalSeconds;
                    double processWidth = processDuration * ((width - processBorderWidth) / totalProcessDuration);

                    // create tooltip
                    process.WindowTitles.RemoveAll(w => string.IsNullOrWhiteSpace(w) || string.IsNullOrEmpty(w));
                    string windowTitle = process.WindowTitles.Count > 0 ? string.Join(Environment.NewLine, process.WindowTitles) : "[no window titles]";
                    string tooltip =    "From " + process.Start.ToLongTimeString() + " to " + process.End.ToLongTimeString() + Environment.NewLine
                                        + "Process: " + ProcessNameHelper.GetFileDescription(process.ProcessName) + Environment.NewLine 
                                        + "Window Titles: " + windowTitle + Environment.NewLine + Environment.NewLine 
                                        + "Keystrokes: " + process.NumberOfKeystrokes + Environment.NewLine 
                                        + "Mouse clicks: " + process.NumberOfMouseClicks;
                    
                    bool visibility = lastProcess.Equals(process) ? false : true;
                    processRectangles.Add(new ProcessRectangle { Data = process, Width = processWidth, Height = 30, X = processX, Tooltip = tooltip, IsVisible = visibility });
                    processX += (processWidth + ProcessRectangle.TaskBoundaryWidth);
                }

                bool isUserDefined = task.TaskDetectionCase == TaskDetectionCase.Missing ? true : false;
                TaskRectangle taskRectangle = new TaskRectangle(task) { X = x, Width = width, Height = 30, ProcessRectangle = processRectangles, TaskName = task.TaskTypeValidated, Timestamp = task.End.ToShortTimeString(), IsUserDefined = isUserDefined };
                RectItems.Add(taskRectangle);
                x += (width + TaskRectangle.TaskBoundaryWidth);
            }

            StringToBrushConverter.UpdateColors(RectItems);
            DrawLegend();
        }
        
        public void DrawLegend()
        {
            // clear old legend
            Legend.Children.Clear();
            Legend.RowDefinitions.Clear();

            // draw new legend
            int count = 0;
            var numColumns = 6;
            var usedColors = StringToBrushConverter.GetColorPallette();

            var numberOfRowsNeeded = Math.Ceiling(usedColors.Keys.Count / (double)numColumns);
            for (int i = 0; i < numberOfRowsNeeded; i++)
            {
                Legend.RowDefinitions.Add(new RowDefinition());
            }

            foreach (string key in usedColors.Keys)
            {
                Brush legendColor;
                usedColors.TryGetValue(key, out legendColor);
                if (legendColor == null) continue;

                StackPanel colorPanel = new StackPanel();
                colorPanel.Orientation = Orientation.Horizontal;
                colorPanel.Margin = new Thickness(5);

                Rectangle colorRectangle = new Rectangle();
                colorRectangle.Fill = legendColor;
                colorRectangle.Height = 20;
                colorRectangle.Width = 20;
                colorRectangle.Margin = new Thickness(0, 0, 5, 0);
                colorPanel.Children.Add(colorRectangle);
                    
                TextBlock colorText = new TextBlock();
                colorText.Inlines.Add(key);
                colorPanel.Children.Add(colorText);

                colorPanel.SetValue(Grid.RowProperty, count / numColumns);
                colorPanel.SetValue(Grid.ColumnProperty, count % numColumns);
                Legend.Children.Add(colorPanel);
                count++;
            }
        }

        private void RedrawTimeline()
        {
            RectItems.Clear();
            GenerateRectangles();
        }

        #endregion

        #region UI handlers

        private void RedrawTimelineEvent(object sender, MouseEventArgs e)
        {
            GenerateRectangles();
        }

        /// <summary>
        /// Called when the save button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (TaskDetection task in _tasks)
            {
                Trace.WriteLine(task);
                //TODO: actual save
            }
            Trace.WriteLine("Comments: " + Comments.Text);
            DialogResult = true;
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawTimeline();
        }

        //Called when a user clicks on a task boundary
        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var process = ((sender as Rectangle).DataContext as ProcessRectangle).Data;
            
            foreach (TaskDetection task in _tasks)
            {
                int index = task.TimelineInfos.FindIndex(p => p.Equals(process));
                if (index != -1 && (index + 1) < task.TimelineInfos.Count)
                {
                    ExtractProcessesFromTask(task, task.TimelineInfos.GetRange(++index, task.TimelineInfos.Count - index));
                    break;
                }
            }
        }

        //Called when the user wants to deleted a user-defined task boundary
        private void DeleteTaskBoundaryButton_Click(object sender, RoutedEventArgs e)
        {
            var task = ((sender as Button).DataContext as TaskRectangle).Data;
            var index = _tasks.FindIndex(t => t.Equals(task));

            TaskDetection taskToAdd = null;

            if (index != -1 && index + 1 < _tasks.Count)
            {
                taskToAdd = _tasks.ElementAt(++index);
            }
            else if (index != -1 && index - 1 >= 0)
            {
                taskToAdd = _tasks.ElementAt(--index);
            }

            if (taskToAdd != null)
            {
                AddProcessesToAnotherTask(task, taskToAdd, task.TimelineInfos);
            }
        }

        //Called when the user validates a task boundary (correct boundary)
        private void RadioButton_Checked_Correct(object sender, RoutedEventArgs e)
        {
            var task = ((sender as RadioButton).DataContext as TaskRectangle).Data;
            TaskDetectionCase previousTaskDetectionCase = task.TaskDetectionCase;
            task.TaskDetectionCase = TaskDetectionCase.Correct;

            if (previousTaskDetectionCase == TaskDetectionCase.Wrong)
            {
                var index = _tasks.FindIndex(t => t.Equals(task));
                if (index != -1 && index + 1 < _tasks.Count)
                {
                    var nextTask = _tasks.ElementAt(++index);
                    nextTask.TaskTypeValidated = string.Empty;
                }
            }
            ValidateSaveButtonEnabled();
        }

        //Called when the user validates a task boundary (wrong boundary)
        private void RadioButton_Checked_Incorrect(object sender, RoutedEventArgs e)
        {
            var task = ((sender as RadioButton).DataContext as TaskRectangle).Data;
            task.TaskDetectionCase = TaskDetectionCase.Wrong;
            var index = _tasks.FindIndex(t => t.Equals(task));
            if (index != -1 && index + 1 < _tasks.Count)
            {
                var nextTask = _tasks.ElementAt(++index);
                nextTask.TaskTypeValidated = task.TaskTypeValidated;
            }
            
            ValidateSaveButtonEnabled();
        }

        #endregion

        #region Add and remove processes

        /// <summary>
        /// Extract a process from a task and at it to a newly created task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="processes"></param>
        private void ExtractProcessesFromTask(TaskDetection task, List<TaskDetectionInput> processes)
        {
            //Add process to new task
            TaskDetection newTask = new TaskDetection();
            newTask.TimelineInfos = processes;
            newTask.Start = newTask.TimelineInfos.First().Start;
            newTask.End = newTask.TimelineInfos.Last().End;
            newTask.TaskTypeValidated = string.Empty;
            newTask.TaskDetectionCase = task.TaskDetectionCase;

            //Remove process from old task
            task.TimelineInfos.RemoveAll(process => processes.Contains(process));
            task.TimelineInfos.Sort();
            task.Start = task.TimelineInfos.First().Start;
            task.End = task.TimelineInfos.Last().End;
            task.TaskDetectionCase = TaskDetectionCase.Missing;
            
            //Add new task to list of tasks
            _tasks.Add(newTask);
            _tasks.Sort();

            RedrawTimeline();
        }
        
        //Remove process from a task and add it to another task
        private void AddProcessesToAnotherTask(TaskDetection oldTask, TaskDetection newTask, List<TaskDetectionInput> processes)
        {
            _tasks.Remove(oldTask);

            newTask.TimelineInfos.AddRange(processes);
            newTask.TimelineInfos.Sort();
            newTask.Start = newTask.TimelineInfos.First().Start;
            newTask.End = newTask.TimelineInfos.Last().End;
            newTask.TaskTypeValidated = oldTask.TaskTypeValidated;

            _tasks.Sort();

            RedrawTimeline();
        }
        #endregion

        #region Save button validation

        /// <summary>
        /// Validate whether the save button should be enabled
        /// </summary>
        private void ValidateSaveButtonEnabled()
        {
            foreach (var task in _tasks)
            {
                if (task.TaskDetectionCase == TaskDetectionCase.NotValidated)
                {
                    ValidationComplete = false;
                    break;
                }
                ValidationComplete = true;
            }
        }
        #endregion
    }
}