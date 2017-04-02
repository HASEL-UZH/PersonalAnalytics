// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-24
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskDetectionTracker.Views.Converters
{
   
    public class StringToBrushConverter : IValueConverter
    {
        
        private static BrushConverter converter = new BrushConverter();

        private static Brush[] brushes = new Brush[]
        {
            (Brush) converter.ConvertFromString("#247BA0"),
            (Brush) converter.ConvertFromString("#70C1B3"),
            (Brush) converter.ConvertFromString("#B2DBBF"),
            (Brush) converter.ConvertFromString("#F3FFBD"),
            (Brush) converter.ConvertFromString("#FF1654"),
            (Brush) converter.ConvertFromString("#50514F"),
            (Brush) converter.ConvertFromString("#F25F5C"),
            (Brush) converter.ConvertFromString("#FFE066"),
            (Brush) converter.ConvertFromString("#247BA0"),
            (Brush) converter.ConvertFromString("#70C1B3")
        };
        
        private static Dictionary<string, Brush> colorMapper = new Dictionary<string, Brush>();
        
        public static Dictionary<string, Brush> GetUsedColors()
        {
            return colorMapper;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush color;
            bool hasKey = colorMapper.TryGetValue(value.ToString(), out color);
            if (!hasKey)
            {
                color = brushes[colorMapper.Keys.Count % brushes.Length];
                colorMapper.Add(value.ToString(), color);
            }
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        internal static void UpdateColors(ObservableCollection<TaskRectangle> rectItems)
        {
            List<String> usedNames = new List<string>();

            foreach (TaskRectangle task in rectItems)
            {
                if (!usedNames.Contains(task.TaskName))
                {
                    usedNames.Add(task.TaskName);
                }
                foreach (ProcessRectangle process in task.ProcessRectangle)
                {
                    if (!usedNames.Contains(process.ProcessName))
                    {
                        usedNames.Add(process.ProcessName);
                    }
                }
            }

            try
            {
                foreach (String key in colorMapper.Keys)
                {
                    if (!usedNames.Contains(key))
                    {
                        colorMapper.Remove(key);
                    }
                }
            }
            catch (Exception e) { }
        }
    }
}