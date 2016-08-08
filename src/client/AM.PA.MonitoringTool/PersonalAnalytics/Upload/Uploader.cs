// Created by André Meyer at MSR
// Created: 2016-01-27
// 
// Licensed under the MIT License. 

using Shared;
using Shared.Data;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows;

namespace PersonalAnalytics.Upload
{
    public class Uploader
    {
        private string _uploadDestinationFolder = @"<Add URL of Shared Windows Server Folder here>";
        private const string _errorTitle = "Upload Wizard: An error occurred";
        private string _participantId;
        private const string _additionalInfoFilePath = "additional_info.txt";
        internal const string _prefix = "upload";

        internal bool ValidateParticipantId(string participantId)
        {
            _participantId = participantId;

            var remotePath = GetUploadFilePath();

            var dirExists = RemoteDirectoryExists(remotePath);
            if (!dirExists) return false;

            var writeAccess = HasWritePermissionOnDir(remotePath);

            return dirExists && writeAccess;
        }

        private bool RemoteDirectoryExists(string path)
        {
            try
            {
                var di = new DirectoryInfo(path);
                return (di.Exists);
            }
            catch
            {
                return false;
            }
        }

        private bool HasWritePermissionOnDir(string path)
        {
            try
            {
                var writeAllow = false;
                var writeDeny = false;
                var accessControlList = Directory.GetAccessControl(path);
                if (accessControlList == null)
                    return false;
                var accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                        continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        writeAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        writeDeny = true;
                }

                return writeAllow && !writeDeny;
            }
            catch
            {
                return false;
            }
        }

        internal string AnonymizeCollectedData(bool obfuscateMeetingTitles, bool obfuscateWindowTitles)
        {
            try
            {
                // copy file
                var anonymizedDbFile = CreateCopyOfOriginalDbFile();
                if (string.IsNullOrEmpty(anonymizedDbFile)) return string.Empty;

                // anonymize it (if necessary
                if (obfuscateMeetingTitles || obfuscateWindowTitles)
                {
                    anonymizedDbFile = RunAnonymization(anonymizedDbFile, obfuscateMeetingTitles, obfuscateWindowTitles);
                }

                return anonymizedDbFile;
            }
            catch (Exception e)
            {
                AskToSendErrorMessage(e, "AnonymizeCollectedData", "obfuscating the collected data");
                return string.Empty;
            }
        }

        private string CreateCopyOfOriginalDbFile()
        {
            try
            {
                var anonymizedDbFile = Path.Combine(Settings.ExportFilePath, "pa_obfuscated.dat");
                var originalDbFile = Database.GetLocalDatabaseSavePath();
                File.Copy(originalDbFile, anonymizedDbFile, true);

                return anonymizedDbFile;
            }
            catch (Exception e)
            {
                AskToSendErrorMessage(e, "CreateCopyOfOriginalDbFile", "copying the original database file");
                return string.Empty;
            }
        }

        private string RunAnonymization(string anonymizedDbFile, bool obfuscateMeetingTitles, bool obfuscateWindowTitles)
        {
            var anonDb = new DatabaseImplementation(anonymizedDbFile);
            anonDb.Connect();

            // log info about obfuscation
            var msg = string.Format(CultureInfo.InvariantCulture, "Starting obfuscation for participant {0} (obfuscateMeetingTitles={1}, obfuscateWindowTitles={2}).", _participantId, obfuscateMeetingTitles, obfuscateWindowTitles);
            Database.GetInstance().LogInfo(msg);
            anonDb.LogInfo(msg);

            // anonymize Meeting Titles if user desires
            if (obfuscateMeetingTitles)
            {
                try
                {
                    var query = "UPDATE " + Settings.MeetingsTable + " SET subject = hash(subject);";
                    var res = anonDb.ExecuteDefaultQuery(query);
                    LogSuccessfulObfuscation(anonDb, Settings.MeetingsTable, res);
                }
                catch (Exception e)
                {
                    AskToSendErrorMessage(e, "obfuscateMeetingTitles", "obfuscating the meeting titles", anonDb);
                }
            }

            // anonymize Window Titles if user desires
            if (obfuscateWindowTitles)
            {
                try
                {
                    var query = "UPDATE " + Settings.WindowsActivityTable + " SET window = hash(window);";
                    var res = anonDb.ExecuteDefaultQuery(query);
                    LogSuccessfulObfuscation(anonDb, Settings.WindowsActivityTable, res);
                }
                catch (Exception e)
                {
                    AskToSendErrorMessage(e, "obfuscateWindowTitles", "obfuscating the window titles", anonDb);
                }
            }

            anonDb.Disconnect();

            return anonymizedDbFile;
        }

        private void LogSuccessfulObfuscation(DatabaseImplementation anonDb, string windowsActivityTable, int res)
        {
            var logMsg = string.Format(CultureInfo.InvariantCulture, "Successfully obfucscated table '{0}' with {1} entries.", windowsActivityTable, res);
            anonDb.LogInfo(logMsg);
            Database.GetInstance().LogInfo(logMsg);
        }

        internal string CreateUploadZip(string anonymizedDbFilePath)
        {
            try
            {
                var zipFileName = "pa_upload_" + _participantId + ".zip";
                var zipFilePath = Path.Combine(Settings.ExportFilePath, zipFileName);
                if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

                using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    var dbFileName = "pa_" + _participantId + ".dat";
                    archive.CreateEntryFromFile(anonymizedDbFilePath, dbFileName);

                    var logFile = Logger.GetLogPath();
                    if (File.Exists(logFile)) archive.CreateEntryFromFile(logFile, "errors.log");

                    var additionalInfo = GetAdditionalInfoFilePath();
                    if (File.Exists(additionalInfo)) archive.CreateEntryFromFile(additionalInfo, _additionalInfoFilePath);
                }

                return zipFilePath;
            }
            catch (Exception e)
            {
                AskToSendErrorMessage(e, "CreateUploadZip", "preparing the zip file for the upload");
                return string.Empty;
            }
        }

        internal string UploadZip(string sourceZipFilePath)
        {
            var timeStamp = DateTime.Now.Ticks;
            var zipFileName = "pa_" + _participantId + "_" + timeStamp + ".zip";
            var destinationZipFilePath = Path.Combine(GetUploadFilePath(), zipFileName);

            // perform the copy (to shared folder)
            try
            {
                Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "Started copying the anonymized collected data from {0} to {1}.", sourceZipFilePath, destinationZipFilePath));
                File.Copy(sourceZipFilePath, destinationZipFilePath, false);
                Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "Finished copying the anonymized collected data from {0} to {1}.", sourceZipFilePath, destinationZipFilePath));
                return zipFileName;
            }
            catch (Exception e)
            {
                AskToSendErrorMessage(e, "UploadZip", "uploading the obfuscated data to our shared drive");
                return string.Empty;
            }
        }

        private void AskToSendErrorMessage(Exception e, string methodName, string methodDescription, DatabaseImplementation anonDb = null)
        {
            // log
            Logger.WriteToLogFile(e);
            var msg = string.Format(CultureInfo.InvariantCulture, "An error occurred in the upload wizard (method: {0}): {1}.", methodName, e.Message);
            Database.GetInstance().LogError(msg);
            if (anonDb != null) anonDb.LogError(msg);

            // ask user to send info
            var res = MessageBox.Show("We are sorry, but there was an error " + methodDescription + ". Please try again.\n\nDo you want to notify us via email to quickly resolve this issue?", _errorTitle, MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes) Retrospection.Handler.GetInstance().SendFeedback(_errorTitle + " (" + methodName + ")", "Error:\n\n" + e.Message + "\n\n" + e.StackTrace);
        }

        private string GetUploadFilePath()
        {
            return Path.Combine(_uploadDestinationFolder, _participantId);
        }

        internal string GetAdditionalInfoFilePath()
        {
            return Path.Combine(Settings.ExportFilePath, _additionalInfoFilePath);
        }

        internal bool RunQuickUpload()
        {
            try
            {
                var obfuscateMeetingTitles = Database.GetInstance().GetSettingsBool(_prefix + "RBObfuscateMeetingTitles", false);
                var obfuscateWindowTitles = Database.GetInstance().GetSettingsBool(_prefix + "RBObfuscateWindowTitles", false);

                var anonymizedDbFilePath = AnonymizeCollectedData(obfuscateMeetingTitles, obfuscateWindowTitles);
                if (string.IsNullOrEmpty(anonymizedDbFilePath)) throw new Exception("An error occured when anonymizing the collected data.");

                var _localZipFilePath = CreateUploadZip(anonymizedDbFilePath);
                if (string.IsNullOrEmpty(_localZipFilePath)) throw new Exception("An error occured when preparing the data (zip-file) for the upload.");

                var _uploadZipFileName = UploadZip(_localZipFilePath);
                if (string.IsNullOrEmpty(_uploadZipFileName)) throw new Exception("An error occured when uploading the data (zip-file).");

                return true;
            }
            catch (Exception e)
            {
                AskToSendErrorMessage(e, "RunQuickUpload", e.Message);
                return false;
            }
        }
    }
}
