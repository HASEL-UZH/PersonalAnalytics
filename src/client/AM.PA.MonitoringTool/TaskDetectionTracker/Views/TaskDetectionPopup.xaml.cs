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
using Shared.Data;
using Shared.Helpers;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window
    {
        private DispatcherTimer _popUpReminderTimer;
        public ObservableCollection<TaskRectangle> RectItems { get; set; }
        public static double TimelineWidth { get; set; }
        public int Confidence_TaskSwitch = -1;
        public int Confidence_TaskType = -1;
        private bool CancelValidationForced;
        public bool ValidationComplete { get; set; }

        internal List<TaskDetection> TaskSwitchesValidated = new List<TaskDetection>();
        internal List<TaskDetection> TaskSwitchesNotValidated;
        private readonly List<TaskDetection> _taskSwitchesInTimeline;
        private double _totalTimePostponed;
        public string PostponedInfo;

        /// <inheritdoc />
        /// <summary>
        /// Create a new Popup with the tasks in the parameter
        /// </summary>
        /// <param name="taskSwitches"></param>
        /// <param name="isCurrentPopupFirstTimeWithPredictions"></param>
        public TaskDetectionPopup(List<TaskDetection> taskSwitches, bool isCurrentPopupFirstTimeWithPredictions)
        {
            InitializeComponent();

            // preserve task switch list for later (deep copy!)
            //this._taskSwitches_NotValidated = taskSwitches.ConvertAll(task => new TaskDetection(task.Start, task.End, task.TaskTypeProposed, task.TaskTypeValidated, task.TimelineInfos, task.IsMainTask));

            TaskSwitchesNotValidated = new List<TaskDetection>();
            foreach (var task in taskSwitches)
            {
                var taskNew_TimeLineInfos = task.TimelineInfos.ConvertAll(info => new TaskDetectionInput());
                var taskNew = new TaskDetection(task.Start, task.End, task.TaskTypeProposed, task.TaskTypeValidated, taskNew_TimeLineInfos, task.IsMainTask);
                TaskSwitchesNotValidated.Add(taskNew);
            }

            //Event handlers
            //this.Deactivated += Window_Deactivated;
            this.StateChanged += Window_StateChanged;
            this.Closing += Window_OnClosing;
            this.SizeChanged += Window_SizeChanged;

            //Set task context
            Step1_Timeline.DataContext = this;
            Step2_Timeline.DataContext = this;
            //Step3_Save_Button.DataContext = this;

            //Create timeline
            this._taskSwitchesInTimeline = taskSwitches;

            // find ideal timeline width
            var minDuration = _taskSwitchesInTimeline.Min(t => t.TimelineInfos.Min(p => p.End.Subtract(p.Start))).TotalSeconds;
            var totalDuration = _taskSwitchesInTimeline.Sum(t => t.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds));
            var timeLineWidth = totalDuration / minDuration * Settings.MinimumProcessWidth;
            if (timeLineWidth > Settings.MaximumTimeLineWidth) TimelineWidth = Settings.MaximumTimeLineWidth;
            else if (timeLineWidth < Settings.MinimumProcessTime_Seconds) TimelineWidth = Settings.MinimumTimeLineWidth;
            else TimelineWidth = timeLineWidth;
            
            RectItems = new ObservableCollection<TaskRectangle>();
            this.Loaded += TaskDetectionPopup_Loaded;
            GenerateRectangles();

            // show hint for study participant
            EmphasizeTaskDetectionIsEnabled.Visibility = (isCurrentPopupFirstTimeWithPredictions ? Visibility.Visible : Visibility.Collapsed);

            // start survey
            GoToStep(1);
        }

        private void TaskDetectionPopup_Loaded(object sender, RoutedEventArgs e)
        {
            DrawTaskTypeLegend();
        }

        #region Handle PopUp Response (Reminder, avoid closing before validated, etc.)

        /// <summary>
        /// Prevent the window from closing. Minimize it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_OnClosing(object sender, CancelEventArgs e)
        {
            // if pop-up not filled out correctly, cancel and minimize it
            if (CancelValidationForced == false && (DialogResult == false || ValidationComplete == false))
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
            switch (WindowState)
            {
                // minimize event is handed by deactivated event already
                case WindowState.Minimized:
                    if (!_skipNextMinimizedEvent)
                    {
                        Database.GetInstance().LogInfo(Settings.TrackerName + ": User minimized the PopUp.");
                        PostponedInfo += FormatPostponedInfo(Settings.PopUpReminderInterval_Short.ToString());
                        StartReminderTimer(Settings.PopUpReminderInterval_Short);
                    }
                    _skipNextMinimizedEvent = false;
                    break;
                case WindowState.Maximized:
                case WindowState.Normal:
                    StopReminderTimer();
                    RedrawTimeline();
                    break;
            }
        }

        /// <summary>
        /// When the window loses focus, start the pop-up reminder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void Window_Deactivated(object sender, EventArgs e)
        //{
        //    //if (! _skipNextMinimizedEvent)
        //    //{
        //    //    Database.GetInstance().LogInfo(Settings.TrackerName + ": User minimized the PopUp.");
        //    //    PostponedInfo += FormatPostponedInfo(Settings.PopUpReminderInterval_Short.ToString());
        //    //    StartReminderTimer(Settings.PopUpReminderInterval_Short);
        //    //}

        //    //_skipNextMinimizedEvent = false;
        //}

        private void StartReminderTimer(TimeSpan reminderInterval)
        {
            if (_popUpReminderTimer != null)
            {
                _popUpReminderTimer.Stop();
                _popUpReminderTimer = null;
            }

            _popUpReminderTimer = new DispatcherTimer();
            _popUpReminderTimer.Interval = reminderInterval;
            _popUpReminderTimer.Tick += PopUpReminder_Tick;
            _popUpReminderTimer.Start();

            _totalTimePostponed += reminderInterval.TotalMinutes;
        }

        private void StopReminderTimer()
        {
            if (_popUpReminderTimer == null) return;
            _popUpReminderTimer.Stop();
            _popUpReminderTimer = null;
        }

        private void PopUpReminder_Tick(object sender, EventArgs e)
        {
            StopReminderTimer();

            // only show pop-up if its from the same day and not postponed for too long
           if (_totalTimePostponed <= Settings.MaximumTimePostponed_Minutes && _taskSwitchesInTimeline.First().Start.Date == DateTime.Now.Date)
            {
                BegForParticipation.Visibility = Visibility.Visible;
                WindowState = WindowState.Normal;
                this.Topmost = true;
                this.Activate();
            }
            // else, close it (and show another one later)
            else
            {
                ForceCloseValidation();
            }
        }

        private bool _skipNextMinimizedEvent = false;

        private void ValidationPostponed_Short_Click(object sender, RoutedEventArgs e)
        {
            Database.GetInstance().LogInfo(Settings.TrackerName + ": User postponed the PopUp by " + Settings.PopUpReminderInterval_Short + ".");
            PostponedInfo += FormatPostponedInfo(Settings.PopUpReminderInterval_Short.ToString());
            _skipNextMinimizedEvent = true;
            WindowState = WindowState.Minimized;
            StartReminderTimer(Settings.PopUpReminderInterval_Short);
        }

        private void ValidationPostponed_Long_Click(object sender, RoutedEventArgs e)
        {
            Database.GetInstance().LogInfo(Settings.TrackerName + ": User postponed the PopUp by " + Settings.PopUpReminderInterval_Long + ".");
            PostponedInfo += FormatPostponedInfo(Settings.PopUpReminderInterval_Long.ToString());
            _skipNextMinimizedEvent = true;
            WindowState = WindowState.Minimized;
            StartReminderTimer(Settings.PopUpReminderInterval_Long); // overwrites the timer interval
        }

        private void ValidationCanceled_Click(object sender, RoutedEventArgs e)
        {
            Database.GetInstance().LogInfo(Settings.TrackerName + ": User canceled the PopUp.");
            PostponedInfo += FormatPostponedInfo("Canceled by User");

            StopReminderTimer();
            ForceCloseValidation();
        }

        private static string FormatPostponedInfo(string message = "")
        {
            return string.Format("[{0}: {1}], ", DateTime.Now.ToString("HH:mm:ss"), message);
        }

        private void ForceCloseValidation()
        {
            CancelValidationForced = true;
            ValidationComplete = false;
            //DialogResult = true;
            Close();
        }

        #endregion

        #region Draw Timeline & Legend

        /// <summary>
        /// Draws the timeline
        /// </summary>
        private void GenerateRectangles()
        {
            //margin on the left and right side of the timeline
            const double margin = 20;
            var totalTaskBorderSpace = _taskSwitchesInTimeline.Count * TaskRectangle.TaskBoundaryWidth;

            var totalDuration = _taskSwitchesInTimeline.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
            var totalWidth = TimelineWidth - (2 * margin) - totalTaskBorderSpace;
            var x = margin;

            //draw each task
            for (var i = 0; i < _taskSwitchesInTimeline.Count; i++)
            {
                var task = _taskSwitchesInTimeline.ElementAt(i);
                var duration = task.End.Subtract(task.Start).TotalSeconds;
                var width = duration * (totalWidth / totalDuration);
            
                var processRectangles = new ObservableCollection<ProcessRectangle>();
                var totalProcessDuration = task.TimelineInfos.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
                var processBorderWidth = (task.TimelineInfos.Count - 1) * ProcessRectangle.TaskBoundaryWidth;

                var processX = 0.0;
                var lastProcess = task.TimelineInfos.Last();

                //draw each process
                foreach (var process in task.TimelineInfos)
                {
                    var processDuration = process.End.Subtract(process.Start).TotalSeconds;
                    //if (processDuration < Settings.MinimumProcessTime_Seconds) continue; // only visualize processes longer than 10s
                    var processWidth = processDuration * ((width - processBorderWidth) / totalProcessDuration);

                    // create tooltip
                    process.WindowTitles.RemoveAll(w => string.IsNullOrWhiteSpace(w) || string.IsNullOrEmpty(w));
                    var windowTitle = process.WindowTitles.Count > 0 ? string.Join(Environment.NewLine, process.WindowTitles) : "[no window titles]";
                    var tooltip =    "From " + process.Start.ToLongTimeString() + " to " + process.End.ToLongTimeString() + Environment.NewLine
                                        + "Process: " + process.ProcessNameFormatted + Environment.NewLine //ProcessNameHelper.GetFileDescription(process.ProcessName) + Environment.NewLine 
                                        + "Window Titles: " + windowTitle + Environment.NewLine + Environment.NewLine 
                                        + "Keystrokes: " + process.NumberOfKeystrokes + Environment.NewLine 
                                        + "Mouse clicks: " + process.NumberOfMouseClicks;
                    
                    var visibility = ! lastProcess.Equals(process);
                    processRectangles.Add(new ProcessRectangle { Data = process, Width = processWidth, Height = Settings.ProcessHeight, X = processX, Tooltip = tooltip, IsVisible = visibility });
                    processX += (processWidth + ProcessRectangle.TaskBoundaryWidth);
                }

                var isUserDefined = (task.TaskDetectionCase == TaskDetectionCase.Missing);
                var taskRectangle = new TaskRectangle(task) { X = x, Width = width, Height = Settings.TaskHeight, ProcessRectangle = processRectangles, TaskName = task.TaskTypeValidated, Timestamp = task.End.ToShortTimeString(), IsUserDefined = isUserDefined };
                RectItems.Add(taskRectangle);
                x += (width + TaskRectangle.TaskBoundaryWidth);
            }

            StringToBrushConverter.UpdateColors(RectItems);
            //DrawTaskTypeLegend();
        }
        
        /// <summary>
        /// Draws a legend for the task types
        /// </summary>
        public void DrawTaskTypeLegend()
        {
            return; // disabled legend for the moment

            // clear old legend
            TaskTypeLegend.Children.Clear();
            TaskTypeLegend.RowDefinitions.Clear();

            // get legend items (the color-list contains processes and tasks, just have tasks)
            var tasktypevalues = Enum.GetValues(typeof(TaskTypes));
            var usedColors = StringToBrushConverter.GetColorPallette();
            var legendList = new List<string>();

            foreach (var item in tasktypevalues)
            {
                if (usedColors.ContainsKey(item.ToString())) legendList.Add(item.ToString());
            }

            // draw new legend
            var count = 0;
            const int numColumns = 6;

            var numberOfRowsNeeded = Math.Ceiling(legendList.Count / (double)numColumns);
            for (var i = 0; i < numberOfRowsNeeded; i++)
            {
                TaskTypeLegend.RowDefinitions.Add(new RowDefinition());
            }

            foreach (var key in legendList)
            {
                Brush legendColor;
                usedColors.TryGetValue(key, out legendColor);
                if (legendColor == null) continue;

                var colorPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };
                var colorRectangle = new Rectangle
                {
                    Fill = legendColor,
                    Height = 20,
                    Width = 20,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                colorPanel.Children.Add(colorRectangle);
                    
                var colorText = new TextBlock();
                var text = ProcessNameHelper.GetFileDescription(key);
                colorText.Inlines.Add(text);
                colorPanel.Children.Add(colorText);

                colorPanel.SetValue(Grid.RowProperty, count / numColumns);
                colorPanel.SetValue(Grid.ColumnProperty, count % numColumns);
                TaskTypeLegend.Children.Add(colorPanel);
                count++;
            }
        }

        private void RedrawTimeline()
        {
            RectItems.Clear();
            GenerateRectangles();
        }

        #endregion

        #region UI handlers (Steps)

        #region Step 1: Validate Task Switches

        private void Step1_Initialize()
        {
            SetWindowHeader("Step 1/3: Please validate your task switches");
            Step1_Timeline_ScrollViewer.ScrollToLeftEnd();
        }

        /// <summary>
        /// Will enable the save button when the user scrolled to the end
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Step1_Timeline_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.HorizontalOffset == scrollViewer.ScrollableWidth)
                Step1_Next_Button.IsEnabled = true;
        }

        private void Step1_Next_Button_Click(object sender, RoutedEventArgs e)
        {
            GoToStep(2);
        }

        /// <summary>
        /// Called when the user changes the combobox selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // redraw legend when item is selected
            DrawTaskTypeLegend();
        }

        private void RedrawTimelineEvent(object sender, MouseEventArgs e)
        {
            GenerateRectangles();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawTimeline();
        }

        /// <summary>
        /// Called when a user clicks on a task boundary to ENABLE it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsTaskSwitch_ButtonClick(object sender, MouseButtonEventArgs e)
        {
            var process = ((sender as Rectangle).DataContext as ProcessRectangle).Data;
            AddTaskBoundary(process);
        }

        /// <summary>
        /// Called when a user clicks on a task boundary to DISABLE it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsNoTaskSwitch_ButtonClick(object sender, MouseButtonEventArgs e)
        {
            var task = ((sender as Rectangle).DataContext as TaskRectangle).Data;

            // don't remove the last item
            if (task == _taskSwitchesInTimeline.Last())
            {
                MessageBox.Show("You cannot remove this last task switch item as this was the time the pop-up showed up, which is a switch to the study.", "Warning", MessageBoxButton.OK);
            }
            // it's save to remove it
            else
            {
                RemoveTaskBoundary(task);
            }
        }

        #endregion

        #region Step 2: validate task types

        private void Step2_Initialize()
        {
            SetWindowHeader("Step 2/3: Please validate your task types");
            Step2_Timeline_ScrollViewer.ScrollToLeftEnd();
        }

        /// <summary>
        /// Will enable the save button when the user scrolled to the end
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Step2_Timeline_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var Step2_ScrollViewer = (ScrollViewer)sender;
            if (Step2_ScrollViewer.HorizontalOffset == Step2_ScrollViewer.ScrollableWidth)
                Step2_Next_Button.IsEnabled = true;
        }

        private void Step2_Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            GoToStep(1);
        }

        private void Step2_Next_Button_Click(object sender, RoutedEventArgs e)
        {
            GoToStep(3);
        }

        #endregion

        #region Step 3: mini questionnaire & comments & save

        private void Step3_Initialize()
        {
            SetWindowHeader("Step 3/3: Almost done!");
            EmphasizeTaskDetectionIsEnabled.Visibility = Visibility.Collapsed;
        }

        private void Step3_Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            GoToStep(2);
        }

        private void Step3_Save_Button_Click(object sender, RoutedEventArgs e)
        {
            FinalizeValidations();
            ValidationComplete = true;
            DialogResult = true;
            Close();
        }

        #region Handle Confidence Ratings

        private void Confidence_TaskSwitch_ResetButtons()
        {
            TaskSwitchConfidence_Btn_1.Background = Brushes.White;
            TaskSwitchConfidence_Btn_2.Background = Brushes.White;
            TaskSwitchConfidence_Btn_3.Background = Brushes.White;
            TaskSwitchConfidence_Btn_4.Background = Brushes.White;
            TaskSwitchConfidence_Btn_5.Background = Brushes.White;

            TaskSwitchConfidence_Btn_1.Foreground = Brushes.Black;
            TaskSwitchConfidence_Btn_2.Foreground = Brushes.Black;
            TaskSwitchConfidence_Btn_3.Foreground = Brushes.Black;
            TaskSwitchConfidence_Btn_4.Foreground = Brushes.Black;
            TaskSwitchConfidence_Btn_5.Foreground = Brushes.Black;
        }

        private void Confidence_TaskSwitch_1_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskSwitch_ResetButtons();
            Confidence_TaskSwitch = 1;
            TaskSwitchConfidence_Btn_1.Background = Shared.Settings.RetrospectionColorBrush;
            TaskSwitchConfidence_Btn_1.Foreground = Brushes.White;
        }

        private void Confidence_TaskSwitch_2_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskSwitch_ResetButtons();
            Confidence_TaskSwitch = 2;
            TaskSwitchConfidence_Btn_2.Background = Shared.Settings.RetrospectionColorBrush;
            TaskSwitchConfidence_Btn_2.Foreground = Brushes.White;
        }

        private void Confidence_TaskSwitch_3_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskSwitch_ResetButtons();
            Confidence_TaskSwitch = 3;
            TaskSwitchConfidence_Btn_3.Background = Shared.Settings.RetrospectionColorBrush;
            TaskSwitchConfidence_Btn_3.Foreground = Brushes.White;

        }

        private void Confidence_TaskSwitch_4_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskSwitch_ResetButtons();
            Confidence_TaskSwitch = 4;
            TaskSwitchConfidence_Btn_4.Background = Shared.Settings.RetrospectionColorBrush;
            TaskSwitchConfidence_Btn_4.Foreground = Brushes.White;
        }

        private void Confidence_TaskSwitch_5_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskSwitch_ResetButtons();
            Confidence_TaskSwitch = 5;
            TaskSwitchConfidence_Btn_5.Background = Shared.Settings.RetrospectionColorBrush;
            TaskSwitchConfidence_Btn_5.Foreground = Brushes.White;
        }

        private void Confidence_TaskType_ResetButtons()
        {
            TaskTypeConfidence_Btn_1.Background = Brushes.White;
            TaskTypeConfidence_Btn_2.Background = Brushes.White;
            TaskTypeConfidence_Btn_3.Background = Brushes.White;
            TaskTypeConfidence_Btn_4.Background = Brushes.White;
            TaskTypeConfidence_Btn_5.Background = Brushes.White;

            TaskTypeConfidence_Btn_1.Foreground = Brushes.Black;
            TaskTypeConfidence_Btn_2.Foreground = Brushes.Black;
            TaskTypeConfidence_Btn_3.Foreground = Brushes.Black;
            TaskTypeConfidence_Btn_4.Foreground = Brushes.Black;
            TaskTypeConfidence_Btn_5.Foreground = Brushes.Black;
        }

        private void Confidence_TaskType_1_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskType_ResetButtons();
            Confidence_TaskType = 1;
            TaskTypeConfidence_Btn_1.Background = Shared.Settings.RetrospectionColorBrush;
            TaskTypeConfidence_Btn_1.Foreground = Brushes.White;
        }

        private void Confidence_TaskType_2_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskType_ResetButtons();
            Confidence_TaskType = 2;
            TaskTypeConfidence_Btn_2.Background = Shared.Settings.RetrospectionColorBrush;
            TaskTypeConfidence_Btn_2.Foreground = Brushes.White;
        }

        private void Confidence_TaskType_3_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskType_ResetButtons();
            Confidence_TaskType = 3;
            TaskTypeConfidence_Btn_3.Background = Shared.Settings.RetrospectionColorBrush;
            TaskTypeConfidence_Btn_3.Foreground = Brushes.White;

        }

        private void Confidence_TaskType_4_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskType_ResetButtons();
            Confidence_TaskType = 4;
            TaskTypeConfidence_Btn_4.Background = Shared.Settings.RetrospectionColorBrush;
            TaskTypeConfidence_Btn_4.Foreground = Brushes.White;
        }

        private void Confidence_TaskType_5_Checked(object sender, RoutedEventArgs e)
        {
            Confidence_TaskType_ResetButtons();
            Confidence_TaskType = 5;
            TaskTypeConfidence_Btn_5.Background = Shared.Settings.RetrospectionColorBrush;
            TaskTypeConfidence_Btn_5.Foreground = Brushes.White;
        }

        #endregion

        #endregion

        private void GoToStep(int step)
        {
            // first hide all
            Step1_TaskSwitches.Visibility = Visibility.Collapsed;
            Step2_TaskTypes.Visibility = Visibility.Collapsed;
            Step3_Confidence.Visibility = Visibility.Collapsed;

            // then make step 'step' visible
            switch (step)
            {
                case 1:
                    Step1_Initialize();
                    Step1_TaskSwitches.Visibility = Visibility.Visible;
                    break;
                case 2:
                    Step2_Initialize();
                    Step2_TaskTypes.Visibility = Visibility.Visible;
                    break;
                case 3:
                    Step3_Initialize();
                    Step3_Confidence.Visibility = Visibility.Visible;
                    break;
                default:
                    Step1_TaskSwitches.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SetWindowHeader(string message = "Please add and validate your Task-Switches")
        {
            if (_taskSwitchesInTimeline != null && _taskSwitchesInTimeline.Count > 0)
                message += " from " + _taskSwitchesInTimeline.First().Start.ToShortTimeString() + " to " + _taskSwitchesInTimeline.Last().End.ToShortTimeString() + "";

            WindowTitleBar.Text = message;
        }

        #endregion

        #region Add and remove processes

        /// <summary>
        /// This method is called when the user hits 'save'. All task detections with the state "NotValidated"
        /// are now considered to be Correct and their status is changed accordingly.
        /// </summary>
        private void FinalizeValidations()
        {
            try
            {

                // all tasks that are not yet validated are correct
                foreach (var task in _taskSwitchesInTimeline)
                {
                    // not yet validated items are "correct"
                    if (task.TaskDetectionCase == TaskDetectionCase.NotValidated) task.TaskDetectionCase = TaskDetectionCase.Correct;

                    // save "correct" and "missing" items to validated list
                    TaskSwitchesValidated.Add(task);
                }

                // remaining ones were wrong (add to final list)
                foreach (var task in TaskSwitchesNotValidated)
                {
                    var taskAlreadySaved = false;

                    foreach (var taskValidated in TaskSwitchesValidated)
                    {
                        if (task.End == taskValidated.End)
                        {
                            taskAlreadySaved = true;
                            break;
                        }
                    }

                    // add tasks that were missing or are correct to final list
                    if (!taskAlreadySaved)
                    {
                        task.TaskDetectionCase = TaskDetectionCase.Wrong;
                        TaskSwitchesValidated.Add(task);
                    }
                }

                // sort list again
                TaskSwitchesValidated.Sort();
            }
            catch (Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        /// <param name="process"></param>
        private void AddTaskBoundary(TaskDetectionInput process)
        {
            foreach (TaskDetection task in _taskSwitchesInTimeline)
            {
                int index = task.TimelineInfos.FindIndex(p => p.Equals(process));
                if (index != -1 && (index + 1) < task.TimelineInfos.Count)
                {
                    ExtractProcessesFromTask(task, task.TimelineInfos.GetRange(++index, task.TimelineInfos.Count - index));
                    break;
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        /// <param name="task"></param>
        private void RemoveTaskBoundary(TaskDetection task)
        {
            var index = _taskSwitchesInTimeline.FindIndex(t => t.Equals(task));

            TaskDetection taskToAdd = null;

            if (index != -1 && index + 1 < _taskSwitchesInTimeline.Count)
            {
                taskToAdd = _taskSwitchesInTimeline.ElementAt(++index);
            }
            else if (index != -1 && index - 1 >= 0)
            {
                taskToAdd = _taskSwitchesInTimeline.ElementAt(--index);
            }

            if (taskToAdd != null)
            {
                AddProcessesToAnotherTask(task, taskToAdd, task.TimelineInfos);
            }
        }

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
            if (processes.Count > 0)
            {
                newTask.Start = newTask.TimelineInfos.First().Start;
                newTask.End = newTask.TimelineInfos.Last().End;
            }
            newTask.TaskTypeValidated = TaskTypes.Other;
            newTask.TaskDetectionCase = task.TaskDetectionCase;

            //Remove process from old task
            task.TimelineInfos.RemoveAll(process => processes.Contains(process));
            task.TimelineInfos.Sort();
            task.Start = task.TimelineInfos.First().Start;
            task.End = task.TimelineInfos.Last().End;
            task.TaskDetectionCase = TaskDetectionCase.Missing;
            
            //Add new task to list of tasks
            _taskSwitchesInTimeline.Add(newTask);
            _taskSwitchesInTimeline.Sort();

            RedrawTimeline();
        }
        
        /// <summary>
        /// Remove process from a task and add it to another task
        /// </summary>
        /// <param name="oldTask"></param>
        /// <param name="newTask"></param>
        /// <param name="processes"></param>
        private void AddProcessesToAnotherTask(TaskDetection oldTask, TaskDetection newTask, List<TaskDetectionInput> processes)
        {
            _taskSwitchesInTimeline.Remove(oldTask);

            newTask.TimelineInfos.AddRange(processes);
            newTask.TimelineInfos.Sort();
            newTask.Start = newTask.TimelineInfos.First().Start;
            newTask.End = newTask.TimelineInfos.Last().End;
            newTask.TaskTypeValidated = oldTask.TaskTypeValidated;

            _taskSwitchesInTimeline.Sort();

            RedrawTimeline();
        }

        #endregion

    }
}