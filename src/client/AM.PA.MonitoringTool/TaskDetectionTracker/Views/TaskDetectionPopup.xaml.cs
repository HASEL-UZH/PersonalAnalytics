// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using TaskDetectionTracker.Model;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// Interaction logic for TaskDetectionPopup.xaml
    /// </summary>
    public partial class TaskDetectionPopup : Window
    {
        private List<TaskDetectionInput> _processes;

        public ObservableCollection<ProcessRectangle> RectItems { get; set; }

        public TaskDetectionPopup(List<TaskDetectionInput> processes)
        {
            this._processes = processes;
            RectItems = new ObservableCollection<ProcessRectangle> { new ProcessRectangle { X = 100, Y = 100, Width = 100, Height = 100 }, new ProcessRectangle { X = 150, Y = 150, Width = 150, Height = 150 } };
           
            InitializeComponent();
            Timeline.DataContext = this;
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
