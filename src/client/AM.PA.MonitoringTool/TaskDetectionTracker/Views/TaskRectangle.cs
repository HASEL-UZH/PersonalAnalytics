// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using TaskDetectionTracker.Model;

namespace TaskDetectionTracker.Views
{
    /// <summary>
    /// View Model for tasks
    /// </summary>
    public class TaskRectangle : INotifyPropertyChanged
    {
        public TaskRectangle(TaskDetection data)
        {
            _data = data;
            data.PropertyChanged += Data_PropertyChanged;
        }

        private void Data_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("TaskName");
        }

        private TaskDetection _data;

        //Task rectangle
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string TaskName { get { return Data.TaskTypeValidated; } set { Data.TaskTypeValidated = value; } }
        public string Timestamp { get; set; }
        public bool IsUserDefined { get; set; }
        public TaskDetection Data { get { return _data; } set { _data = value; } }
        public string ID { get { return TaskName + Timestamp; } }
        public TaskDetectionCase TaskDetectionCase { get { return Data.TaskDetectionCase; } set { Data.TaskDetectionCase = value; OnPropertyChanged("TaskDetectionCase"); } }
        public bool IsMainTask { get { return Data.IsMainTask; } set { Data.IsMainTask = value; } }

        //Task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }

        private static double _taskCheckboxWidth = 15;
        
        public static double TaskCheckboxWidth { get { return _taskCheckboxWidth; } }
        
        //Process rectangle
        public ObservableCollection<ProcessRectangle> ProcessRectangle { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    /// <summary>
    /// View model for processes
    /// </summary>
    public class ProcessRectangle {

        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public string Tooltip { get; set; }
        public TaskDetectionInput Data { get; set; }
        public string ProcessName { get { return Data.ProcessName; } set { Data.ProcessName = value; } }

        //Potential task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }
        public bool IsVisible { get; set; }

    }
}