// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Media;
using TaskDetectionTracker.Model;

namespace TaskDetectionTracker.Views
{
    public class TaskRectangle
    {
        //Task rectangle
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; }
        public string TaskName { get { return Data.TaskTypeValidated; } set { Data.TaskTypeValidated = value; } }
        public string Timestamp { get; set; }
        public bool IsUserDefined { get; set; }
        public TaskDetection Data { get; set; }
        public string ID { get { return TaskName + Timestamp; } }
        public TaskDetectionCase TaskDetectionCase {get { return Data.TaskDetectionCase;} set {Data.TaskDetectionCase = value;}}

        //Task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }

        private static double _taskCheckboxWidth = 15;
        public static double TaskCheckboxWidth { get { return _taskCheckboxWidth; } }
        
        //Process rectangle
        public ObservableCollection<ProcessRectangle> ProcessRectangle { get; set; }
    }

    public class ProcessRectangle {

        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public Brush Color { get; set; }
        public string Tooltip { get; set; }
        public TaskDetectionInput Data { get; set; }

        //Potential task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }
        public bool IsVisible { get; set; }

    }
}