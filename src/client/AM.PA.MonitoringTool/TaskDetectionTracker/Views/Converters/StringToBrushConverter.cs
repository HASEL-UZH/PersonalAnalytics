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
            (Brush) converter.ConvertFromString("#FFFF00"),
            (Brush) converter.ConvertFromString("#1CE6FF"),
            (Brush) converter.ConvertFromString("#FF34FF"),
            (Brush) converter.ConvertFromString("#FF4A46"),
            (Brush) converter.ConvertFromString("#008941"),
            (Brush) converter.ConvertFromString("#006FA6"),
            (Brush) converter.ConvertFromString("#A30059"),
            (Brush) converter.ConvertFromString("#FFDBE5"),
            (Brush) converter.ConvertFromString("#7A4900"),
            (Brush) converter.ConvertFromString("#0000A6"),
            (Brush) converter.ConvertFromString("#63FFAC"),
            (Brush) converter.ConvertFromString("#B79762"),
            (Brush) converter.ConvertFromString("#004D43"),
            (Brush) converter.ConvertFromString("#8FB0FF"),
            (Brush) converter.ConvertFromString("#997D87"),
            (Brush) converter.ConvertFromString("#5A0007"),
            (Brush) converter.ConvertFromString("#809693"),
            (Brush) converter.ConvertFromString("#FEFFE6"),
            (Brush) converter.ConvertFromString("#1B4400"),
            (Brush) converter.ConvertFromString("#4FC601"),
            (Brush) converter.ConvertFromString("#3B5DFF"),
            (Brush) converter.ConvertFromString("#4A3B53"),
            (Brush) converter.ConvertFromString("#FF2F80"),
            (Brush) converter.ConvertFromString("#61615A"),
            (Brush) converter.ConvertFromString("#BA0900"),
            (Brush) converter.ConvertFromString("#6B7900"),
            (Brush) converter.ConvertFromString("#00C2A0"),
            (Brush) converter.ConvertFromString("#FFAA92"),
            (Brush) converter.ConvertFromString("#FF90C9"),
            (Brush) converter.ConvertFromString("#B903AA"),
            (Brush) converter.ConvertFromString("#D16100"),
            (Brush) converter.ConvertFromString("#DDEFFF"),
            (Brush) converter.ConvertFromString("#000035"),
            (Brush) converter.ConvertFromString("#7B4F4B"),
            (Brush) converter.ConvertFromString("#A1C299"),
            (Brush) converter.ConvertFromString("#300018"),
            (Brush) converter.ConvertFromString("#0AA6D8"),
            (Brush) converter.ConvertFromString("#013349"),
            (Brush) converter.ConvertFromString("#00846F"),
            (Brush) converter.ConvertFromString("#372101"),
            (Brush) converter.ConvertFromString("#FFB500"),
            (Brush) converter.ConvertFromString("#C2FFED"),
            (Brush) converter.ConvertFromString("#A079BF"),
            (Brush) converter.ConvertFromString("#CC0744"),
            (Brush) converter.ConvertFromString("#C0B9B2"),
            (Brush) converter.ConvertFromString("#C2FF99"),
            (Brush) converter.ConvertFromString("#001E09"),
            (Brush) converter.ConvertFromString("#00489C"),
            (Brush) converter.ConvertFromString("#6F0062"),
            (Brush) converter.ConvertFromString("#0CBD66"),
            (Brush) converter.ConvertFromString("#EEC3FF"),
            (Brush) converter.ConvertFromString("#456D75"),
            (Brush) converter.ConvertFromString("#B77B68"),
            (Brush) converter.ConvertFromString("#7A87A1"),
            (Brush) converter.ConvertFromString("#788D66"),
            (Brush) converter.ConvertFromString("#885578"),
            (Brush) converter.ConvertFromString("#FAD09F"),
            (Brush) converter.ConvertFromString("#FF8A9A"),
            (Brush) converter.ConvertFromString("#D157A0"),
            (Brush) converter.ConvertFromString("#BEC459"),
            (Brush) converter.ConvertFromString("#456648"),
            (Brush) converter.ConvertFromString("#0086ED"),
            (Brush) converter.ConvertFromString("#886F4C"),
            (Brush) converter.ConvertFromString("#34362D"),
            (Brush) converter.ConvertFromString("#B4A8BD"),
            (Brush) converter.ConvertFromString("#00A6AA"),
            (Brush) converter.ConvertFromString("#452C2C"),
            (Brush) converter.ConvertFromString("#636375"),
            (Brush) converter.ConvertFromString("#A3C8C9"),
            (Brush) converter.ConvertFromString("#FF913F"),
            (Brush) converter.ConvertFromString("#938A81"),
            (Brush) converter.ConvertFromString("#575329"),
            (Brush) converter.ConvertFromString("#00FECF"),
            (Brush) converter.ConvertFromString("#B05B6F"),
            (Brush) converter.ConvertFromString("#8CD0FF"),
            (Brush) converter.ConvertFromString("#3B9700"),
            (Brush) converter.ConvertFromString("#04F757"),
            (Brush) converter.ConvertFromString("#C8A1A1"),
            (Brush) converter.ConvertFromString("#1E6E00"),
            (Brush) converter.ConvertFromString("#7900D7"),
            (Brush) converter.ConvertFromString("#A77500"),
            (Brush) converter.ConvertFromString("#6367A9"),
            (Brush) converter.ConvertFromString("#A05837"),
            (Brush) converter.ConvertFromString("#6B002C"),
            (Brush) converter.ConvertFromString("#772600"),
            (Brush) converter.ConvertFromString("#D790FF"),
            (Brush) converter.ConvertFromString("#9B9700"),
            (Brush) converter.ConvertFromString("#549E79"),
            (Brush) converter.ConvertFromString("#FFF69F"),
            (Brush) converter.ConvertFromString("#201625"),
            (Brush) converter.ConvertFromString("#72418F"),
            (Brush) converter.ConvertFromString("#BC23FF"),
            (Brush) converter.ConvertFromString("#99ADC0"),
            (Brush) converter.ConvertFromString("#3A2465"),
            (Brush) converter.ConvertFromString("#922329"),
            (Brush) converter.ConvertFromString("#5B4534"),
            (Brush) converter.ConvertFromString("#FDE8DC"),
            (Brush) converter.ConvertFromString("#404E55"),
            (Brush) converter.ConvertFromString("#0089A3"),
            (Brush) converter.ConvertFromString("#CB7E98"),
            (Brush) converter.ConvertFromString("#A4E804"),
            (Brush) converter.ConvertFromString("#324E72"),
            (Brush) converter.ConvertFromString("#6A3A4C")

            //(Brush) converter.ConvertFromString("#e6194b"),
            //(Brush) converter.ConvertFromString("#3cb44b"),
            //(Brush) converter.ConvertFromString("#ffe119"),
            //(Brush) converter.ConvertFromString("#0082c8"),
            //(Brush) converter.ConvertFromString("#f58231"),
            //(Brush) converter.ConvertFromString("#911eb4"),
            //(Brush) converter.ConvertFromString("#46f0f0"),
            //(Brush) converter.ConvertFromString("#f032e6"),
            //(Brush) converter.ConvertFromString("#d2f53c"),
            //(Brush) converter.ConvertFromString("#fabebe"),
            //(Brush) converter.ConvertFromString("#008080"),
            //(Brush) converter.ConvertFromString("#e6beff"),
            //(Brush) converter.ConvertFromString("#aa6e28"),
            //(Brush) converter.ConvertFromString("#fffac8"),
            //(Brush) converter.ConvertFromString("#800000"),
            //(Brush) converter.ConvertFromString("#aaffc3"),
            //(Brush) converter.ConvertFromString("#808000"),
            //(Brush) converter.ConvertFromString("#ffd8b1"),
            //(Brush) converter.ConvertFromString("#000080"),
            //(Brush) converter.ConvertFromString("#808080")
        };
        
        private static Dictionary<string, Brush> colorMapper = new Dictionary<string, Brush>();
        
        public static Dictionary<string, Brush> GetColorPallette()
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
            List<string> usedNames = new List<string>();

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
                foreach (string key in colorMapper.Keys)
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