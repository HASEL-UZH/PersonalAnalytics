// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shared.Data
{
    //public static class RemoteDataHandler
    //{
    //    /// <summary>
    //    /// remote data is used if a file with the same naming exists in the 
    //    /// subdirectory 'remote'
    //    /// </summary>
    //    /// <returns></returns>
    //    public static bool VisualizeWithRemoteData()
    //    {
    //        var fileExist = File.Exists(Database.GetRemoteDatabaseSavePath());
    //        return fileExist;
    //    }

    //    /// <summary>
    //    /// Try to find the best way to merge the productivity data.
    //    /// Merge both lists (if not equal entries)
    //    /// </summary>
    //    /// <param name="dataLocal"></param>
    //    /// <param name="dataRemote"></param>
    //    /// <returns></returns>
    //    public static List<ProductivityTimeDto> MergeProductivityData(List<ProductivityTimeDto> dataLocal, List<ProductivityTimeDto> dataRemote)
    //    {
    //        if (dataLocal.Count == 0) return dataRemote;
    //        if (dataRemote.Count == 0) return dataLocal;

    //        // add all items to dataLocal (if they are not the same)
    //        foreach (var r in dataRemote.Where(r => dataLocal.FindIndex(l => l.Time == r.Time) < 0))
    //        {
    //            dataLocal.Add(r);
    //        }

    //        return dataLocal;
    //    }

    //    /// <summary>
    //    /// Try to find the best way to merge the tasks data.
    //    /// Merge both lists (if not equal entries)
    //    /// Hint: both have still the start-of-day value that shouldn't be considered
    //    /// </summary>
    //    /// <param name="dataLocal"></param>
    //    /// <param name="dataRemote"></param>
    //    /// <returns></returns>
    //    public static List<TasksWorkedOnTimeDto> MergeTasksData(List<TasksWorkedOnTimeDto> dataLocal, List<TasksWorkedOnTimeDto> dataRemote)
    //    {
    //        if (dataLocal.Count <= 1) return dataRemote;
    //        if (dataRemote.Count <= 1) return dataLocal;

    //        // add all items to dataLocal (if they are not the same)
    //        foreach (var r in dataRemote.Where(r => dataLocal.FindIndex(l => l.Time == r.Time) < 0 && r.TasksWorkedOn != 0))
    //        {
    //            dataLocal.Add(r);
    //        }

    //        return dataLocal;
    //    }

    //    /// <summary>
    //    /// Try to find the best way to merge the task timeline data.
    //    /// Merge both lists (if not equal entries)
    //    /// </summary>
    //    /// <param name="dataLocal"></param>
    //    /// <param name="dataRemote"></param>
    //    /// <returns></returns>
    //    public static Dictionary<string, List<StartEndTimeDto>> MergeTasksTimelineData(Dictionary<string, List<StartEndTimeDto>> dataLocal, Dictionary<string, List<StartEndTimeDto>> dataRemote)
    //    {
    //        if (dataLocal.Count == 0) return dataRemote;
    //        if (dataRemote.Count == 0) return dataLocal;

    //        // add all items to dataLocal (if they are not the same)
    //        foreach (var rKey in dataRemote.Keys)
    //        {
    //            if (dataLocal.ContainsKey(rKey))
    //            {
    //                var localItems = dataLocal[rKey];
    //                var remoteItems = dataRemote[rKey];

    //                foreach (var r in remoteItems.Where(r => localItems.FindIndex(l => l.EndTime == r.EndTime) < 0))
    //                {
    //                    dataLocal[rKey].AddRange(dataRemote[rKey]);
    //                }
    //            }
    //            else
    //            {
    //                dataLocal.Add(rKey, dataRemote[rKey]);
    //            }
    //        }

    //        return dataLocal;
    //    }

    //    /// <summary>
    //    /// Try to find the best way to merge the task activity data.
    //    /// Merge both lists (if not equal entries):
    //    /// 1. mstsc (remote desktop) and add data from remote tracker
    //    /// </summary>
    //    /// <param name="dataLocal"></param>
    //    /// <param name="dataRemote"></param>
    //    /// <returns></returns>
    //    public static Dictionary<string, long> MergeActivityData(Dictionary<string, long> dataLocal, Dictionary<string, long> dataRemote)
    //    {
    //        if (dataLocal.Count == 0) return dataRemote;
    //        if (dataRemote.Count == 0) return dataLocal;

    //        foreach (var item in dataLocal)
    //        {
    //            if (item.Key.ToLower().Contains("mstsc"))
    //            {
    //                MergeRemotedesktopData(dataLocal, dataRemote, item);
    //                break;
    //            }
    //        }

    //        return dataLocal;
    //    }

    //    private static void MergeRemotedesktopData(Dictionary<string, long> dataLocal, Dictionary<string, long> dataRemote, KeyValuePair<string, long> item)
    //    {
    //        // delete item
    //        dataLocal.Remove(item.Key);

    //        // add data from remote computer (should be the same share than mstsc process)
    //        foreach (var r in dataRemote)
    //        {
    //            if (r.Key.Equals(Dict.Idle)) continue;

    //            if (dataLocal.ContainsKey(r.Key))
    //            {
    //                dataLocal[r.Key] += r.Value;
    //            }
    //            else
    //            {
    //                dataLocal.Add(r.Key, r.Value);
    //            }
    //        }
    //    }
    //}
}
