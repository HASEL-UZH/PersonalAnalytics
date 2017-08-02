// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaskDetectionTracker.Model;

namespace TaskDetectionTrackerTests
{
    public class DataLoader
    {

        public static List<TaskDetectionInput> LoadTestData()
        {
            var input = new List<TaskDetectionInput>();
            try
            {
                string path = Environment.CurrentDirectory + @"\testdata.csv";

                using (TextFieldParser parser = new TextFieldParser(path))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        input.Add(new TaskDetectionInput { Start = DateTime.Parse(fields[1], CultureInfo.InvariantCulture), WindowTitles = new List<string> { fields[2] }, ProcessName = fields[3] });
                    }
                }

                SetTimestamps(input);
            }
            catch (Exception e) { }
            return input;
        }

        /// <summary>
        /// Sets the timestamps for each process
        /// </summary>
        /// <param name="processes"></param>
        private static void SetTimestamps(List<TaskDetectionInput> processes)
        {
            for (int i = 0; i < processes.Count - 1; i++)
            {
                processes.ElementAt(i).End = processes.ElementAt(i + 1).Start;
            }
        }
    }
}