// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using TaskDetectionTracker.Model;
using System.Linq;
using System;
using TaskDetectionTracker.Data;

namespace TaskDetectionTracker.Helpers
{
    public class DataMerger
    {

        /// <summary>
        /// Merges processes using the following procedure:
        /// 1. Set the end timestamp for each process to the start timestamp of the next process
        /// 2. Set the timestamp of the very last process
        /// 3. Delete all processes that last for less than the minimum threshold (from the Settings)
        /// 4. For the remaining processes, merge all subsequent processses if they are the same
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="totalDuration"></param>
        /// <returns></returns>
        public static List<TaskDetectionInput> MergeProcesses(List<TaskDetectionInput> processes, TimeSpan totalDuration)
        {
            //First set the end timestamp of each process to the value of the start timestamp of the next process
            SetTimestamps(processes);

            //The end timestamp of the last process in the list is equal to the start of the first process + the total duration
            processes.Last().End = processes.First().Start.Add(totalDuration);

            //Delete all processes when the duration is smaller than the treshold
            processes.RemoveAll(p => p.End.Subtract(p.Start).TotalSeconds < Settings.MinimumProcessTimeInSeconds);

            //For the remaining processes, merge all subsequent processes if they are the same
            List<TaskDetectionInput> result = new List<TaskDetectionInput>();

            if (processes.Count > 0)
            {
                var currentGroup = new List<TaskDetectionInput> { processes.First() };

                foreach (var item in processes.Skip(1))
                {
                    if (!currentGroup.First().ProcessName.Equals(item.ProcessName))
                    {
                        //Create new merged process
                        result.Add(new TaskDetectionInput { Start = currentGroup.First().Start, End = currentGroup.Last().End, ProcessName = currentGroup.First().ProcessName, WindowTitles = currentGroup.SelectMany(w => w.WindowTitles).Distinct().ToList() });
                        currentGroup = new List<TaskDetectionInput> { item };
                    }
                    else
                    {
                        currentGroup.Add(item);
                    }
                }
                //Add the last one too
                result.Add(new TaskDetectionInput { Start = currentGroup.First().Start, End = currentGroup.Last().End, ProcessName = currentGroup.First().ProcessName, WindowTitles = currentGroup.SelectMany(w => w.WindowTitles).Distinct().ToList() });
            }

            //LINQ based solution
            /**
            int groupID = -1;

            var result = processes.Select((item, index) =>
            {
                    if (index == 0 || processes[index - 1].ProcessName != item.ProcessName)
                    {
                        ++groupID;
                    }
                    return new { group = groupID, item = item };
                }).GroupBy(item => item.group).Select(group =>
                {
                    return new TaskDetectionInput { ProcessName = group.First().item.ProcessName, Start = group.First().item.Start, End = group.Last().item.End, WindowTitles = group.SelectMany(g => g.item.WindowTitles).Distinct().ToList() };
                }
            ).ToList();**/

            return result;
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

        /// <summary>
        /// Add nubmer of keystrokes and mouse clicks to each process
        /// </summary>
        /// <param name="processes"></param>
        internal static void AddMouseClickAndKeystrokesToProcesses(List<TaskDetectionInput> processes)
        {
            if (processes.Count > 0)
            {
                var clicks = DatabaseConnector.GetMouseClickData(processes.First().Start, processes.Last().End);
                var keys = DatabaseConnector.GetKeystrokeData(processes.First().Start, processes.Last().End);

                foreach (TaskDetectionInput process in processes)
                {
                    //Ignore processes that are shorter than 1 minute
                    if (process.End.Subtract(process.Start).TotalSeconds > 60) //TODO: not too accurate
                    {
                        var clicksForProcess = clicks.Where(c => c.Start >= process.Start && c.Start <= process.End);
                        var keysForProcess = keys.Where(k => k.Start >= process.Start && k.Start <= process.End);
                        process.NumberOfKeystrokes = keysForProcess.Sum(k => k.Keystrokes);
                        process.NumberOfMouseClicks = clicksForProcess.Sum(c => c.Mouseclicks);
                    }
                }
            }
        }
    }

    public struct KeystrokeData
    {
        public DateTime Start;
        public DateTime End;
        public int Keystrokes;
    }

    public struct MouseClickData
    {
        public DateTime Start;
        public DateTime End;
        public int Mouseclicks;
    }
}