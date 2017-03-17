// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Windows.Media;

namespace TaskDetectionTracker.Views
{
    public class ProcessRectangle
    {
        //Process rectangle
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; }
        public string Tooltip { get; set; }

        //Task boundary
        private static double _taskBoundaryWidth = 5;
        public static double TaskBoundaryWidth { get { return _taskBoundaryWidth; } }

        private static double _taskCheckboxWidth = 15;
        public static double TaskCheckboxWidth { get { return _taskCheckboxWidth; } }

        private static double _taskBoundaryTransform = -1 * TaskBoundaryWidth;
        public static double TaskBoundaryTransform { get { return _taskBoundaryTransform; } }
    }
}