// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using TaskDetectionTracker.Model;
using System.Linq;
using System;

namespace TaskDetectionTracker
{
    public class DataMerger
    {

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
            var currentGroup = new List<TaskDetectionInput> { processes.First() };
            
            foreach (var item in processes.Skip(1))
            {
                if (!currentGroup.First().ProcessName.Equals(item.ProcessName))
                {
                    //Create new merged process
                    result.Add(new TaskDetectionInput { Start = currentGroup.First().Start, End = currentGroup.Last().End, ProcessName = currentGroup.First().ProcessName, WindowTitles = currentGroup.SelectMany(w => w.WindowTitles).ToList() });
                    currentGroup = new List<TaskDetectionInput> { item };
                }
                else
                {
                    currentGroup.Add(item);
                }
            }
            //Add the last one too
            result.Add(new TaskDetectionInput { Start = currentGroup.First().Start, End = currentGroup.Last().End, ProcessName = currentGroup.First().ProcessName });
            
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


        private static void SetTimestamps(List<TaskDetectionInput> processes)
        {
            for (int i = 0; i < processes.Count - 1; i++)
            {
                processes.ElementAt(i).End = processes.ElementAt(i + 1).Start;
            }
        }
    }
}