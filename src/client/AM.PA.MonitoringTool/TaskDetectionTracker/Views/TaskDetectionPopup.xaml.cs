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
        private List<TaskDetectionInput> _processes;
        private Dictionary<string, Brush> colors = new Dictionary<string, Brush>();

        public ObservableCollection<ProcessRectangle> RectItems { get; set; }

        public TaskDetectionPopup(List<TaskDetectionInput> processes)
        {
            this._processes = processes;
            InitializeComponent();
            Timeline.DataContext = this;

            StartTime.Inlines.Add(_processes.First().Start.ToShortTimeString());
            EndTime.Inlines.Add(_processes.Last().End.ToShortTimeString());

            RectItems = new ObservableCollection<ProcessRectangle>();
            GenerateRectangles();
        }

        Brush[] brushes = new Brush[]
        {
            Brushes.Black,
            Brushes.Red
        };

        private void GenerateRectangles()
        {
            double totalDuration = _processes.Sum(p => p.End.Subtract(p.Start).TotalSeconds);
            double totalWidth = this.Width;
            double x = 0;

            for (int i = 0; i < _processes.Count; i++)
            {
                TaskDetectionInput process = _processes.ElementAt(i);
                double duration = process.End.Subtract(process.Start).TotalSeconds;
                double width = duration * (totalWidth / totalDuration);
                string tooltip = string.Join(Environment.NewLine, process.WindowTitles) + Environment.NewLine + "Keystrokes: " + process.NumberOfKeystrokes + Environment.NewLine + "Mouse clicks: " + process.NumberOfMouseClicks;

                Brush color;

                bool hasKey = colors.TryGetValue(process.ProcessName, out color);
                if (!hasKey)
                {
                    color = brushes[colors.Keys.Count % brushes.Length];
                    colors.Add(process.ProcessName, color);
                }

                RectItems.Add(new ProcessRectangle { X = x, Y = 100, Width = width, Height = 20, Color = color, Tooltip = tooltip });
                x += width;
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
