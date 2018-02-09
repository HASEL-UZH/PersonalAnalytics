// Created by Katja Kevic (kevic@ifi.uzh.ch) from the University of Zurich
// Created: 2017-05-16
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RDotNet;
using TaskDetectionTracker.Model;
using System.IO;
using TaskDetectionTracker.Properties;
using Shared;
using Shared.Data;

namespace TaskDetectionTracker.Algorithm
{
    public class TaskDetectorImpl : ITaskDetector
    {
        private const double _taskSwitchThreshold = 0.23;

        // Hint 16.08.17: due to deployment issues, the folder "Assets" was also copied (!) to the PersonalAnalytics-main-project and has to be updated as well
        private static string _resourcesFolder = Path.Combine("Assets", "TaskDetectionData"); // was: "Resources"

        private string _taskSwitchDataFolder = "TaskSwitchDataDump";
        private string _taskSwitchDataFileName = "pa-taskswitchdata-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
        private string _taskSwitchDetectionModelFileName = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "taskswitchdetectionmodel.rda");

        private string _taskTypeDataFolder = "TaskTypeDataDump";
        private string _taskTypeDataFileName = "pa-tasktypedata-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
        private string _taskTypeDetectionModelFileName = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "tasktypedetectionmodel.rda");

        private static string _rToolsExtractDirectory = Path.Combine(Environment.CurrentDirectory, _resourcesFolder);
        private static string _rToolsHomeZip = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "R-3.4.0.zip");
        private static string _rToolsLibrariesZip = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "R_libraries.zip");
        private static string _rToolsPath = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "R-3.4.0\\bin\\i386");
        private static string _rToolsHome = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "R-3.4.0");
        private static string _rToolsLibraries = Path.Combine(Environment.CurrentDirectory, _resourcesFolder, "R_libraries");

        public TaskDetectorImpl()
        {
            UnzipRTools();
        }

        /// <summary>
        /// If R-Tools have not yet been unzipped, unzip them
        /// </summary>
        private void UnzipRTools()
        {
            if (!Directory.Exists(_rToolsHome))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(_rToolsHomeZip, _rToolsExtractDirectory);
                Database.GetInstance().LogInfo("Unzipped R Tools to: " + _rToolsExtractDirectory);
            }
            if (!Directory.Exists(_rToolsLibraries))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(_rToolsLibrariesZip, _rToolsExtractDirectory);
                Database.GetInstance().LogInfo("Unzipped R Libraries to: " + _rToolsExtractDirectory);
            }
        }

        /// <summary>
        /// Predicts task switches between <see cref="TaskDetectionInput"/> and predicts task types of <see cref="TaskDetection"/>.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public List<TaskDetection> FindTasks(List<TaskDetectionInput> processes)
        {
            if (processes.Count == 0) return new List<TaskDetection>(); // empty list
            try
            {
                List<Datapoint> dps = new List<Datapoint>();
                foreach (var p in processes)
                {
                    dps.Add(new Datapoint());
                }

                LexicalSimilarities(processes, "window", dps);
                LexicalSimilarities(processes, "process", dps);
                SetKeyStrokeDiffs(processes, dps);
                SetMouseClicksDiff(processes, dps);

                WriteSwitchDetectionFile(dps);
                var tcs = PredictSwitches(processes);
                WriteTypeDetectionFile(tcs);
                PredictTypes(tcs);

                return tcs;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return new List<TaskDetection>(); // empty list
        }

        /// <summary>
        /// Creates a file that arranged the <see cref="Datapoint"/> such that it can be read by the trained classifier
        /// used for predicting the task switches.
        /// </summary>
        /// <param name="dps"></param>
        private void WriteSwitchDetectionFile(List<Datapoint> dps)
        {
            var csv_all = new StringBuilder();

            // write header
            string header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
             "isSuperSwitch", "lexSim1_win", "lexSim2_win", "lexSim3_win", "lexSim4_win",
                      "lexSim1_pro", "lexSim2_pro", "lexSim3_pro", "lexSim4_pro",
                      "totalKeystrokesDiff", "MouseClicksDiff");
            csv_all.AppendLine(header);

            // write raw data
            foreach (Datapoint dp in dps)
            {
                var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", "NA",
                    dp.LexSim1_Win, dp.LexSim2_Win, dp.LexSim3_Win, dp.LexSim4_Win, dp.LexSim1_Pro,
                    dp.LexSim2_Pro, dp.LexSim3_Pro, dp.LexSim4_Pro, dp.TotalKeystrokesDiff,
                     dp.TotalMouseClicksDiff);
                csv_all.AppendLine(newLine);
            }

            // write output to csv file 
            File.WriteAllText(GetTaskDetectionDumpsPath(_taskSwitchDataFolder, _taskSwitchDataFileName), csv_all.ToString(), Encoding.UTF8);
        }

        private void LexicalSimilarities(List<TaskDetectionInput> processes, string feature, List<Datapoint> dps)
        {
            CosineSim cosSim = new CosineSim();
            string[] docs = new string[processes.Count];
            for (int i = 0; i < processes.Count; i++)
            {
                if (feature.Equals("window"))
                {
                    string windows = string.Join(" ", processes[i].WindowTitles);
                    docs[i] = CleanWindowOrProcessString(windows);
                }
                else if (feature.Equals("process"))
                {
                    docs[i] = CleanWindowOrProcessString(processes[i].ProcessName);
                }

            }
            cosSim.TFIDFMeasure(docs);

            #region lexSim1
            if(dps.Count > 0)
            {
                if (feature.Equals("window"))
                {
                    dps[0].LexSim1_Win = 0;
                }
                else if (feature.Equals("process"))
                {
                    dps[0].LexSim1_Pro = 0;
                }

                for (int i = 1; i < processes.Count; i++)
                {
                    if (feature.Equals("window"))
                    {
                        float lexSim1 = cosSim.GetSimilarity(i - 1, i);
                        dps[i].LexSim1_Win = lexSim1;
                    }
                    else if (feature.Equals("process"))
                    {
                        float lexSim1 = cosSim.GetSimilarity(i - 1, i);
                        dps[i].LexSim1_Pro = lexSim1;
                    }
                }
            }

            #endregion
            #region lexSim2
            if (dps.Count > 1)
            {
                if (feature.Equals("window"))
                {
                    dps[0].LexSim2_Win = 0;
                    dps[1].LexSim2_Win = dps[1].LexSim1_Win;
                }
                else if (feature.Equals("process"))
                {
                    dps[0].LexSim2_Pro = 0;
                    dps[1].LexSim2_Pro = dps[1].LexSim1_Pro;
                }

                for (int i = 2; i < processes.Count; i++)
                {
                    if (feature.Equals("window"))
                    {
                        float lexSim2pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim2pre1 = cosSim.GetSimilarity(i - 1, i);
                        float lexSim2 = (lexSim2pre1 + lexSim2pre2) / (float)2;
                        dps[i].LexSim2_Win = lexSim2;
                    }
                    else if (feature.Equals("process"))
                    {
                        float lexSim2pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim2pre1 = cosSim.GetSimilarity(i - 1, i);
                        float lexSim2 = (lexSim2pre1 + lexSim2pre2) / (float)2;
                        dps[i].LexSim2_Pro = lexSim2;
                    }
                }
            }

            #endregion
            #region lexSim3
            if (dps.Count > 2)
            {
                if (feature.Equals("window"))
                {
                    dps[0].LexSim3_Win = 0;
                    dps[1].LexSim3_Win = dps[1].LexSim1_Win;
                    dps[2].LexSim3_Win = dps[2].LexSim2_Win;
                }
                else if (feature.Equals("process"))
                {
                    dps[0].LexSim3_Pro = 0;
                    dps[1].LexSim3_Pro = dps[1].LexSim1_Pro;
                    dps[2].LexSim3_Pro = dps[2].LexSim2_Pro;
                }

                for (int i = 3; i < processes.Count; i++)
                {
                    if (feature.Equals("window"))
                    {
                        float lexSim3pre1 = dps[i].LexSim1_Win;
                        float lexSim3pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim3pre3 = cosSim.GetSimilarity(i - 3, i);
                        dps[i].LexSim3_Win = (lexSim3pre1 + lexSim3pre2 + lexSim3pre3) / (float)3;
                    }
                    else if (feature.Equals("process"))
                    {
                        float lexSim3pre1 = dps[i].LexSim1_Pro;
                        float lexSim3pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim3pre3 = cosSim.GetSimilarity(i - 3, i);
                        dps[i].LexSim3_Pro = (lexSim3pre1 + lexSim3pre2 + lexSim3pre3) / (float)3;
                    }
                }
            }

            #endregion
            #region lexSim4
            if (dps.Count > 3)
            {
                if (feature.Equals("window"))
                {
                    dps[0].LexSim4_Win = 0;
                    dps[1].LexSim4_Win = dps[1].LexSim1_Win;
                    dps[2].LexSim4_Win = dps[2].LexSim2_Win;
                    dps[3].LexSim4_Win = dps[3].LexSim3_Win;
                }
                else if (feature.Equals("process"))
                {
                    dps[0].LexSim4_Pro = 0;
                    dps[1].LexSim4_Pro = dps[1].LexSim1_Pro;
                    dps[2].LexSim4_Pro = dps[2].LexSim2_Pro;
                    dps[3].LexSim4_Pro = dps[3].LexSim3_Pro;
                }

                for (int i = 4; i < processes.Count; i++)
                {
                    if (feature.Equals("window"))
                    {
                        float lexSim4pre1 = dps[i].LexSim1_Win;
                        float lexSim4pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim4pre3 = cosSim.GetSimilarity(i - 3, i);
                        float lexSim4pre4 = cosSim.GetSimilarity(i - 4, i);
                        dps[i].LexSim4_Win = (lexSim4pre1 + lexSim4pre2 + lexSim4pre3 + lexSim4pre4) / (float)4;
                    }
                    else if (feature.Equals("process"))
                    {
                        float lexSim4pre1 = dps[i].LexSim1_Pro;
                        float lexSim4pre2 = cosSim.GetSimilarity(i - 2, i);
                        float lexSim4pre3 = cosSim.GetSimilarity(i - 3, i);
                        float lexSim4pre4 = cosSim.GetSimilarity(i - 4, i);
                        dps[i].LexSim4_Pro = (lexSim4pre1 + lexSim4pre2 + lexSim4pre3 + lexSim4pre4) / (float)4;
                    }
                }
            }
            
            #endregion 
        }

        private string CleanWindowOrProcessString(string window)
        {
            window = window.Replace(',', ' ');
            window = window.Replace('+', ' ');
            window = window.Replace('=', ' ');
            window = window.Replace('"', ' ');
            window = window.Replace('\'', ' ');
            window = window.Replace('-', ' ');
            window = window.Replace('<', ' ');
            window = window.Replace('>', ' ');

            return window;
        }

        private void SetKeyStrokeDiffs(List<TaskDetectionInput> processes, List<Datapoint> dps)
        {
            dps[0].TotalKeystrokesDiff = processes[0].NumberOfKeystrokes;
            for (int i = 1; i < processes.Count; i++)
            {
                dps[i].TotalKeystrokesDiff = Math.Abs(processes[i - 1].NumberOfKeystrokes - processes[i].NumberOfKeystrokes);
            }
        }

        private void SetMouseClicksDiff(List<TaskDetectionInput> processes, List<Datapoint> dps)
        {
            dps[0].TotalMouseClicksDiff = processes[0].NumberOfMouseClicks;
            for (int i = 1; i < processes.Count; i++)
            {
                dps[i].TotalMouseClicksDiff = Math.Abs(processes[i - 1].NumberOfMouseClicks - processes[i].NumberOfMouseClicks);
            }
        }

        /// <summary>
        /// Predicts task switches between <see cref="TaskDetectionInput"/> using a pre-trained classifier.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        private List<TaskDetection> PredictSwitches(List<TaskDetectionInput> processes)
        {
            var tcs = new List<TaskDetection>();
            try
            {
                // set environment variables so R can access the tools
                var path = R_ConvertPathToForwardSlash(_rToolsPath);
                var home = R_ConvertPathToForwardSlash(_rToolsHome);
                REngine.SetEnvironmentVariables(path, home);

                // start REngine
                REngine engine = REngine.GetInstance();

                // read taskswitch-data
                engine.Evaluate("data <- read.csv(file = '" + R_ConvertPathToForwardSlash(GetTaskDetectionDumpsPath(_taskSwitchDataFolder, _taskSwitchDataFileName)) + "', sep = \",\", header = TRUE)");

                // load task switch detection model (model based on previous study, N=14)
                engine.Evaluate("load(\"" + R_ConvertPathToForwardSlash(_taskSwitchDetectionModelFileName) + "\")");
                // use task switch detection model
                GenericVector switchResult = engine.Evaluate("prob <- predict(mod_fit_all, newdata = data, type = \"response\")").AsList();
                // read and process the predictions
                List<TaskDetectionInput> toBundle = new List<TaskDetectionInput>();
                toBundle.Add(processes[0]);

                for (int i = 1; i < switchResult.Count(); i++)
                {
                    NumericVector vec = switchResult[i].AsVector().AsNumeric();

                    if (IsTaskSwitch(vec.First()))
                    {
                        TaskDetection tc = CreateTaskDetectionObject(toBundle);
                        tcs.Add(tc);
                        toBundle.Clear();
                    }

                    toBundle.Add(processes[i]);
                }
                if (toBundle.Count > 0)
                {
                    TaskDetection tc = CreateTaskDetectionObject(toBundle);
                    tcs.Add(tc);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToConsole(e.Message);
                Logger.WriteToLogFile(e);
            }

            return tcs;
        }

        private static bool IsTaskSwitch(double number)
        {
            return (number > _taskSwitchThreshold);
        }

        private TaskDetection CreateTaskDetectionObject(List<TaskDetectionInput> toBundle)
        {
            var tc = new TaskDetection
            {
                Start = toBundle.First().Start,
                End = toBundle.Last().End,
                TimelineInfos = toBundle.ToList()
            };
            return tc;
        }

        /// <summary>
        /// Creates a file that arranges the <see cref="TaskDetection"/> information such that it can be read by the trained 
        /// classifier used for predicting the task types. 
        /// </summary>
        /// <param name="tcs"></param>
        private void WriteTypeDetectionFile(List<TaskDetection> tcs)
        {
            var del = ",";
            #region processCats
            Dictionary<string, string> processCats = new Dictionary<string, string>();
            string[] csv = Resources.ProcessCategories.Split('\n');

            for (int i = 1; i < csv.Length; i++)
            {
                string l = csv[i];
                string[] line = csv[i].Split(',');
                processCats.Add(line[0].Trim(), line[1].Trim());
            }
            HashSet<string> uniqueCategories = new HashSet<string>();
            foreach (var entry in processCats)
            {
                uniqueCategories.Add(entry.Value);
            }
            List<string> distinctProcessCats = uniqueCategories.ToList();

            #endregion

            var header = new StringBuilder();
            foreach (var s in distinctProcessCats)
            {
                header.Append(s + del);
            }
            header.Append("totalKeyStrokes" + del + "Mouseclicks"  );
            StringBuilder csv_types = new StringBuilder();
            csv_types.AppendLine("task"+del+header.ToString());

            foreach(TaskDetection tc in tcs)
            {
                csv_types.Append("1" + del);
                var duration = tc.End - tc.Start;

                #region processCategories
                foreach (String pc in distinctProcessCats)
                {
                    double sum = 0;
                    foreach (var p in tc.TimelineInfos)
                    {
                        if (processCats.ContainsKey(p.ProcessName) && processCats[p.ProcessName].Equals(pc))
                        {
                            var dur = p.End - p.Start;
                            sum += dur.TotalMinutes;
                        }
                    }
                    double avgTimeInProcessCat = sum / duration.TotalMinutes;
                    csv_types.Append(avgTimeInProcessCat + del);
                }
                #endregion

                int sumTotalKeyStrokes = tc.TimelineInfos.Select(d => d.NumberOfKeystrokes).Sum();
                int sumMouseClicks = tc.TimelineInfos.Select(d => d.NumberOfMouseClicks).Sum();

                double keystrokesPerSec = 0;
                if (sumTotalKeyStrokes > 0)
                {
                    keystrokesPerSec = (double)sumTotalKeyStrokes / duration.TotalSeconds;
                }

                double mouseclicksPerSec = 0;
                if (sumMouseClicks > 0)
                {
                    mouseclicksPerSec = (double)sumMouseClicks / duration.TotalSeconds;
                }

                csv_types.Append(keystrokesPerSec + del + mouseclicksPerSec + "\n");

            }
            File.WriteAllText(GetTaskDetectionDumpsPath(_taskTypeDataFolder, _taskTypeDataFileName), csv_types.ToString());
        }

        /// <summary>
        /// Predicts task types of a <see cref="TaskDetection"/> using a pre-trained classifier. The trained task types 
        /// include: Private, Planned Meeting, Unplanned Meeting, Awareness, Planning, Observation, Development, Adminstrative Work
        /// </summary>
        /// <param name="tcs"></param>
        private void PredictTypes(List<TaskDetection> tcs)
        {
            //1: Private, 2: Planned Meeting, 3: Unplanned Meeting, 4: Awareness, 5: Planning, 6: Observation, 7: Development, 8: Adminstrative Work
            try
            {
                // initialize R engine
                var engine = REngine.GetInstance();
                engine.Initialize();

                // read taskswitch-data
                engine.Evaluate("tasktypedata <- read.csv(file = '" + R_ConvertPathToForwardSlash(GetTaskDetectionDumpsPath(_taskTypeDataFolder, _taskTypeDataFileName)) + "', sep = \",\", header = TRUE)");
                engine.Evaluate(".libPaths('" + R_ConvertPathToForwardSlash(_rToolsLibraries) + "')");
                engine.Evaluate("library(randomForest)");

                // load the taskswitch classifier
                engine.Evaluate("load(\"" + R_ConvertPathToForwardSlash(_taskTypeDetectionModelFileName) + "\")");
                // use the classifier 
                var typeResult = engine.Evaluate("prob <- predict(model, newdata = tasktypedata, type = \"response\")").AsList();
                // read the results
                for (var i = 0; i < typeResult.Count(); i++)
                {
                    var resTask = typeResult[i].AsCharacter().First();
                    // convert from string to TaskTypes
                    var type = TaskType.Other;
                    Enum.TryParse(resTask, out type);
                    tcs[i].TaskTypePredicted = type;
                }
            }
            catch(Exception e)
            {
                Logger.WriteToConsole(e.Message);
                Logger.WriteToLogFile(e);
            }
          
          //  engine.Dispose();
        }

        #region File & Path Helpers

        private string GetTaskDetectionDumpsPath(string folder, string filename)
        {
            var path = Shared.Settings.ExportFilePath;
            var dumpFolder = Path.Combine(path, folder);
            if (!Directory.Exists(dumpFolder)) Directory.CreateDirectory(dumpFolder);

            return Path.Combine(dumpFolder, filename);
        }

        private string R_ConvertPathToForwardSlash(string pathWithBackwardSlash)
        {
            return pathWithBackwardSlash.Replace("\\", "/");
        }

        #endregion
    }
}
