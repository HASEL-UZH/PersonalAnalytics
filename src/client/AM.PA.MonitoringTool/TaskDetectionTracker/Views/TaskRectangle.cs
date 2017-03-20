// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Media;

namespace TaskDetectionTracker.Views
{
    public class TaskRectangle
    {
        //Task rectangle
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; }
        public string TaskName { get; set; }

        //Task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }

        private static double _taskCheckboxWidth = 15;
        public static double TaskCheckboxWidth { get { return _taskCheckboxWidth; } }

        private static double _taskBoundaryTransform = -1 * TaskBoundaryWidth;
        public static double TaskBoundaryTransform { get { return _taskBoundaryTransform; } }

        //Process rectangle
        public ObservableCollection<ProcessRectangle> ProcessRectangle { get; set; }
    }

    public class ProcessRectangle {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public Brush Color { get; set; }
        public string Tooltip { get; set; }
    }
}