// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.


using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using FitbitTracker.Model;
using Shared.Data;

namespace FitbitTracker.Data
{
    public class FitbitConnector
    {

        internal static void GetSleepDataForDay(DateTime day)
        {
            try
            {
                string url = "https://api.fitbit.com/1/user/-/sleep/date/today.json";

                WebClient client = new WebClient();
                client.Headers.Add("Authorization", "Bearer " + Database.GetInstance().GetSettingsString(Settings.ACCESS_TOKEN, null));

                Stream data = client.OpenRead(url);

                StreamReader reader = new StreamReader(data);
                string response = reader.ReadToEnd();
               
                SleepData dataObject = JsonConvert.DeserializeObject<SleepData>(response);
               
               

                data.Close();
                reader.Close();
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
