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
using System.Windows.Controls;
using System.Diagnostics;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window, INotifyPropertyChanged
    {
        #region Color definitions
        private static BrushConverter converter = new BrushConverter();

        Brush[] taskBrushes = new Brush[]
        {
            (Brush) converter.ConvertFromString("#247BA0"),
            (Brush) converter.ConvertFromString("#70C1B3"),
            (Brush) converter.ConvertFromString("#B2DBBF"),
            (Brush) converter.ConvertFromString("#F3FFBD"),
            (Brush) converter.ConvertFromString("#FF1654"),
        };

        Brush[] processBrushes = new Brush[]
        {
            (Brush) converter.ConvertFromString("#50514F"),
            (Brush) converter.ConvertFromString("#F25F5C"),
            (Brush) converter.ConvertFromString("#FFE066"),
            (Brush) converter.ConvertFromString("#247BA0"),
            (Brush) converter.ConvertFromString("#70C1B3")
        };

        private Dictionary<string, Brush> taskColors = new Dictionary<string, Brush>();
        private Dictionary<string, Brush> processColors = new Dictionary<string, Brush>();
        #endregion

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
        public static double CanvasWidth { get { return 3000; } }

        public TaskDetectionPopup(List<TaskDetection> tasks)
        {
            InitializeComponent();

            this.StateChanged += Window_StateChanged;
            this.Closing += Window_OnClosing;
            this.SizeChanged += Window_SizeChanged;

            Timeline.DataContext = this;
            Save.DataContext = this;
            
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

        #region Draw Timeline
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

                bool hasKey = taskColors.TryGetValue(task.TaskTypeValidated, out color);
                if (!hasKey)
                {
                    color = taskBrushes[taskColors.Keys.Count % taskBrushes.Length];
                    taskColors.Add(task.TaskTypeValidated, color);
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
                    bool hasProcessKey = processColors.TryGetValue(process.ProcessName, out processColor);
                    if (!hasProcessKey)
                    {
                        processColor = processBrushes[processColors.Keys.Count % processBrushes.Length];
                        processColors.Add(process.ProcessName, processColor);
                    }

                    bool visibility = lastProcess.Equals(process) ? false : true;
                    processRectangles.Add(new ProcessRectangle { Data = process, Width = processWidth, Height = 30, X = processX, Color = processColor, Tooltip = tooltip, IsVisible = visibility });
                    processX += (processWidth + ProcessRectangle.TaskBoundaryWidth);
                }

                bool isUserDefined = task.TaskDetectionCase == TaskDetectionCase.Missing ? true : false;
                RectItems.Add(new TaskRectangle { Data = task, X = x, Width = width, Height = 30, Color = color, ProcessRectangle = processRectangles, TaskName = task.TaskTypeValidated, Timestamp = task.End.ToShortTimeString(), IsUserDefined = isUserDefined });
                x += (width + TaskRectangle.TaskBoundaryWidth);
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (TaskDetection task in _tasks)
            {
                Trace.WriteLine(task);
                //TODO: actual save
            }
            Trace.WriteLine("Comments: " + Comments.Text);
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawTimeline();
        }

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

        private void RadioButton_Checked_Correct(object sender, RoutedEventArgs e)
        {
            var task = ((sender as RadioButton).DataContext as TaskRectangle).Data;
            task.TaskDetectionCase = TaskDetectionCase.Correct;
            var index = _tasks.FindIndex(t => t.Equals(task));
            if (index != -1 && index + 1 < _tasks.Count)
            {
                var nextTask = _tasks.ElementAt(++index);
                nextTask.TaskTypeValidated = string.Empty;
            }

            ValidateSaveButtonEnabled();
        }

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