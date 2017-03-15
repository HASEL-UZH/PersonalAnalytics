// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using TaskDetectionTracker.Model;

namespace TaskDetectionTracker.Views.Tests
{
    [TestClass()]
    public class TaskDetectionPopupTests
    {
        [TestMethod()]
        public void TaskDetectionPopupTest()
        {
            var input = LoadTestData();
            var popup = (Window) new TaskDetectionPopup(input);
            popup.ShowDialog();

           

            Assert.Fail();
        }

        private ObservableCollection<TaskDetectionInput> LoadTestData()
        {
            var input = new ObservableCollection<TaskDetectionInput>();
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
                        input.Add(new TaskDetectionInput { Start = DateTime.Parse(fields[1], CultureInfo.InvariantCulture), WindowTitles = new List<string> { fields[2]}, ProcessName = fields[3] });
                    }
                }
            }
            catch (Exception e) { }
            return input;
        }
    }
}