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
        
        private static readonly BrushConverter Converter = new BrushConverter();

        private static readonly Brush[] Brushes =
        {
            (Brush) Converter.ConvertFromString("#FFFF00"),
            (Brush) Converter.ConvertFromString("#1CE6FF"),
            (Brush) Converter.ConvertFromString("#FF34FF"),
            (Brush) Converter.ConvertFromString("#FF4A46"),
            (Brush) Converter.ConvertFromString("#008941"),
            (Brush) Converter.ConvertFromString("#006FA6"),
            (Brush) Converter.ConvertFromString("#A30059"),
            (Brush) Converter.ConvertFromString("#FFDBE5"),
            (Brush) Converter.ConvertFromString("#7A4900"),
            //(Brush) Converter.ConvertFromString("#0000A6"),
            (Brush) Converter.ConvertFromString("#63FFAC"),
            (Brush) Converter.ConvertFromString("#B79762"),
            //(Brush) Converter.ConvertFromString("#004D43"),
            (Brush) Converter.ConvertFromString("#8FB0FF"),
            (Brush) Converter.ConvertFromString("#997D87"),
            //(Brush) Converter.ConvertFromString("#5A0007"),
            (Brush) Converter.ConvertFromString("#809693"),
            (Brush) Converter.ConvertFromString("#FEFFE6"),
            //(Brush) Converter.ConvertFromString("#1B4400"),
            (Brush) Converter.ConvertFromString("#4FC601"),
            (Brush) Converter.ConvertFromString("#3B5DFF"),
            //(Brush) Converter.ConvertFromString("#4A3B53"),
            (Brush) Converter.ConvertFromString("#FF2F80"),
            (Brush) Converter.ConvertFromString("#61615A"),
            (Brush) Converter.ConvertFromString("#BA0900"),
            (Brush) Converter.ConvertFromString("#6B7900"),
            (Brush) Converter.ConvertFromString("#00C2A0"),
            (Brush) Converter.ConvertFromString("#FFAA92"),
            (Brush) Converter.ConvertFromString("#FF90C9"),
            (Brush) Converter.ConvertFromString("#B903AA"),
            (Brush) Converter.ConvertFromString("#D16100"),
            (Brush) Converter.ConvertFromString("#DDEFFF"),
            (Brush) Converter.ConvertFromString("#000035"),
            (Brush) Converter.ConvertFromString("#7B4F4B"),
            (Brush) Converter.ConvertFromString("#A1C299"),
            (Brush) Converter.ConvertFromString("#300018"),
            (Brush) Converter.ConvertFromString("#0AA6D8"),
            (Brush) Converter.ConvertFromString("#013349"),
            (Brush) Converter.ConvertFromString("#00846F"),
            (Brush) Converter.ConvertFromString("#372101"),
            (Brush) Converter.ConvertFromString("#FFB500"),
            (Brush) Converter.ConvertFromString("#C2FFED"),
            (Brush) Converter.ConvertFromString("#A079BF"),
            (Brush) Converter.ConvertFromString("#CC0744"),
            (Brush) Converter.ConvertFromString("#C0B9B2"),
            (Brush) Converter.ConvertFromString("#C2FF99"),
            (Brush) Converter.ConvertFromString("#001E09"),
            (Brush) Converter.ConvertFromString("#00489C"),
            (Brush) Converter.ConvertFromString("#6F0062"),
            (Brush) Converter.ConvertFromString("#0CBD66"),
            (Brush) Converter.ConvertFromString("#EEC3FF"),
            (Brush) Converter.ConvertFromString("#456D75"),
            (Brush) Converter.ConvertFromString("#B77B68"),
            (Brush) Converter.ConvertFromString("#7A87A1"),
            (Brush) Converter.ConvertFromString("#788D66"),
            (Brush) Converter.ConvertFromString("#885578"),
            (Brush) Converter.ConvertFromString("#FAD09F"),
            (Brush) Converter.ConvertFromString("#FF8A9A"),
            (Brush) Converter.ConvertFromString("#D157A0"),
            (Brush) Converter.ConvertFromString("#BEC459"),
            (Brush) Converter.ConvertFromString("#456648"),
            (Brush) Converter.ConvertFromString("#0086ED"),
            (Brush) Converter.ConvertFromString("#886F4C"),
            (Brush) Converter.ConvertFromString("#34362D"),
            (Brush) Converter.ConvertFromString("#B4A8BD"),
            (Brush) Converter.ConvertFromString("#00A6AA"),
            (Brush) Converter.ConvertFromString("#452C2C"),
            (Brush) Converter.ConvertFromString("#636375"),
            (Brush) Converter.ConvertFromString("#A3C8C9"),
            (Brush) Converter.ConvertFromString("#FF913F"),
            (Brush) Converter.ConvertFromString("#938A81"),
            (Brush) Converter.ConvertFromString("#575329"),
            (Brush) Converter.ConvertFromString("#00FECF"),
            (Brush) Converter.ConvertFromString("#B05B6F"),
            (Brush) Converter.ConvertFromString("#8CD0FF"),
            (Brush) Converter.ConvertFromString("#3B9700"),
            (Brush) Converter.ConvertFromString("#04F757"),
            (Brush) Converter.ConvertFromString("#C8A1A1"),
            (Brush) Converter.ConvertFromString("#1E6E00"),
            (Brush) Converter.ConvertFromString("#7900D7"),
            (Brush) Converter.ConvertFromString("#A77500"),
            (Brush) Converter.ConvertFromString("#6367A9"),
            (Brush) Converter.ConvertFromString("#A05837"),
            (Brush) Converter.ConvertFromString("#6B002C"),
            (Brush) Converter.ConvertFromString("#772600"),
            (Brush) Converter.ConvertFromString("#D790FF"),
            (Brush) Converter.ConvertFromString("#9B9700"),
            (Brush) Converter.ConvertFromString("#549E79"),
            (Brush) Converter.ConvertFromString("#FFF69F"),
            //(Brush) converter.ConvertFromString("#201625"),
            (Brush) Converter.ConvertFromString("#72418F"),
            (Brush) Converter.ConvertFromString("#BC23FF"),
            (Brush) Converter.ConvertFromString("#99ADC0"),
            (Brush) Converter.ConvertFromString("#3A2465"),
            (Brush) Converter.ConvertFromString("#922329"),
            (Brush) Converter.ConvertFromString("#5B4534"),
            (Brush) Converter.ConvertFromString("#FDE8DC"),
            (Brush) Converter.ConvertFromString("#404E55"),
            (Brush) Converter.ConvertFromString("#0089A3"),
            (Brush) Converter.ConvertFromString("#CB7E98"),
            (Brush) Converter.ConvertFromString("#A4E804"),
            (Brush) Converter.ConvertFromString("#324E72"),
            (Brush) Converter.ConvertFromString("#6A3A4C")

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
        
        private static readonly Dictionary<string, Brush> ColorMapper = new Dictionary<string, Brush>();
        
        public static Dictionary<string, Brush> GetColorPallette()
        {
            return ColorMapper;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush color;
            var hasKey = ColorMapper.TryGetValue(value.ToString(), out color);
            if (hasKey) return color;
            color = Brushes[ColorMapper.Keys.Count % Brushes.Length * 2]; // TODO: test * 2
            ColorMapper.Add(value.ToString(), color);
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        internal static void UpdateColors(ObservableCollection<TaskRectangle> rectItems)
        {
            var usedNames = new List<string>();

            foreach (var task in rectItems)
            {
                if (!usedNames.Contains(task.TaskName.ToString()))
                {
                    usedNames.Add(task.TaskName.ToString());
                }
                foreach (var process in task.ProcessRectangle)
                {
                    if (!usedNames.Contains(process.ProcessName))
                    {
                        usedNames.Add(process.ProcessName);
                    }
                }
            }

            try
            {
                foreach (var key in ColorMapper.Keys)
                {
                    if (!usedNames.Contains(key))
                    {
                        ColorMapper.Remove(key);
                    }
                }
            }
            catch (Exception e) {} // no idea what happens here, but avoids exception...
        }
    }
}