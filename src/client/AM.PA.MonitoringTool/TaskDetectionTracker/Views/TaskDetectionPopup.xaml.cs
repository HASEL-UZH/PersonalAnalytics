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

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window
    {
        private List<TaskDetection> _tasks;
        private Dictionary<string, Brush> colors = new Dictionary<string, Brush>();

        public ObservableCollection<TaskRectangle> RectItems { get; set; }

        public TaskDetectionPopup(List<TaskDetection> tasks)
        {
            this._tasks = tasks;
            InitializeComponent();

            Timeline.DataContext = this;
            

            StartTime.Inlines.Add(_tasks.First().Start.ToShortTimeString());
            EndTime.Inlines.Add(_tasks.Last().End.ToShortTimeString());
            
            RectItems = new ObservableCollection<TaskRectangle>();
            GenerateRectangles();
        }

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
            Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RectItems.Clear();
            GenerateRectangles();
        }
    }
}
