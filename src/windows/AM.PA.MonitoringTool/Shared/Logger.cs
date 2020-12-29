// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.IO;

namespace Shared
{
    public static class Logger
    {
        /// <summary>
        /// Writes a message to the console.
        /// </summary>
        /// <param name="message"></param>
        public static void WriteToConsole(string message)
        {
#if DEBUG
            Console.WriteLine("# " + message);
#endif
        }

        /// <summary>
        /// Writes a message from a source to the logfile in the output folder.
        /// </summary>
        /// <param name="e"></param>
        public static void WriteToLogFile(Exception e)
        {
            try 
            {
                using (var w = File.AppendText(GetLogPath()))
                {
                    Log(e, w);
                }
            } catch (Exception) {
                //could not log, problems with the log file
                WriteToConsole("Error while logging, msg: " + e.Message);
            }
        }

        private static void Log(Exception e, TextWriter w)
        {
            w.Write("\r\nLog Entry: ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            w.WriteLine("{0} : {1}", e.Message , e.StackTrace);
            w.WriteLine("-------------------------------");
        }

        /// <summary>
        /// Returns the path to the logfile
        /// </summary>
        /// <returns></returns>
        public static string GetLogPath()
        {
            return Path.Combine(Settings.ExportFilePath, "errors.log");
        }
    }
}
