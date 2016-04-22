// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.IO;

namespace Shared.Data
{
    public class Database
    {
        private static DatabaseImplementation _localDb;
        //private static DatabaseImplementation _remoteDb;

        /// <summary>
        /// Singleton. Returns the instance of the local database.
        /// </summary>
        /// <returns></returns>
        public static DatabaseImplementation GetInstance()
        {
            return _localDb ?? (_localDb = new DatabaseImplementation(GetLocalDatabaseSavePath()));
        }

        /// <summary>
        /// Singleton. Returns the instance of the remote database.
        /// </summary>
        /// <returns></returns>
        //public static DatabaseImplementation GetInstanceRemote()
        //{
        //    if (_remoteDb == null)
        //    {
        //        _remoteDb = new DatabaseImplementation(GetRemoteDatabaseSavePath());
        //        _remoteDb.Connect();
        //    }

        //    return _remoteDb;
        //}

        /// <summary>
        /// Singleton. Returns the instance of the settings database.
        /// </summary>
        /// <returns></returns>
        //public static DatabaseImplementation GetInstanceSettings()
        //{
        //    return _localDb ?? (_localDb = new DatabaseImplementation(GetLocalDatabaseSavePath()));

        //    //if (_settingsDb == null)
        //    //{
        //    //    _settingsDb = new DatabaseImplementation(GetSettingsDatabaseSavePath());
        //    //    _settingsDb.Connect();
        //    //    _settingsDb.CreateSettingsTable();
        //    //}

        //    //return _settingsDb;
        //}

        /// <summary>
        /// Returns the path the (SQLight) database file should be stored on the users
        /// computer. Every week, a new file is created to make it easy to backup and 
        /// 'guarantee' no performance issues.
        /// </summary>
        /// <returns></returns>
        public static string GetLocalDatabaseSavePath()
        {
            return GetDatabaseSavePath(Settings.ExportFilePath);
        }

        /// <summary>
        /// Returns the path of the database file that a RDP user manually copies
        /// there to be merged for the visualizations.
        /// </summary>
        /// <returns></returns>
        //public static string GetRemoteDatabaseSavePath()
        //{
        //    return GetDatabaseSavePath(Settings.ExportFilePath + Settings.RemoteFolderName);
        //}

        /// <summary>
        /// Returns the path of the database file that stores the settings.
        /// </summary>
        /// <returns></returns>
        public static string GetSettingsDatabaseSavePath()
        {
            return GetSettingsSavePath(Settings.ExportFilePath);
        }

        /// <summary>
        /// returns the file name of the database file (path)
        /// 07.04.15: from now on, there is not a weekly dump anymore (to get back to data from previous weeks), but a yearly dump
        /// 20.10.15: from now on, there is not a yearly dump anymore //TODO: check if this results in performance issues
        /// </summary>
        /// <param name="exportPath"></param>
        /// <returns></returns>
        private static string GetDatabaseSavePath(string exportPath)
        {
            return Path.Combine(exportPath, "pa.dat"); // New file name since 20.10.15
            //var dbPath = string.Format(CultureInfo.InvariantCulture,"{0}pa_{1}.dat", exportPath, DateTime.Now.Year); // New file name since 07.04.15
            //var dbPath = string.Format(CultureInfo.InvariantCulture,"{0}pa_{1}_{2}.dat", exportPath, DateTime.Now.Year, Helpers.GetIso8601WeekOfYear(DateTime.Now));
        }

        private static string GetSettingsSavePath(string exportPath)
        {
            return Path.Combine(exportPath, "pa.dat"); // current version: also store settings in normal file
            //return Path.Combine(exportPath, "pa_settings.dat");
        }
    }
}
