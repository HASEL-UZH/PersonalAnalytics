// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

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
        private ObservableCollection<TaskDetectionInput> _input;

        public TaskDetectionPopup(ObservableCollection<TaskDetectionInput> input)
        {
            this._input = input;
            InitializeComponent();
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
