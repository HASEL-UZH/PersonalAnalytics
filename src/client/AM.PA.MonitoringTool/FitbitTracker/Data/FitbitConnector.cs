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
using System.Collections.Specialized;
using System.Text;
using Shared;
using FitbitTracker.Data.FitbitModel;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace FitbitTracker.Data
{
    public enum DataType
    {
        SLEEP, ACTIVITIES, HR
    };

    public class FitbitConnector
    {

        private const string SLEEP_URL = "https://api.fitbit.com/1/user/-/sleep/date/{0}.json";
        private const string REFRESH_URL = "https://api.fitbit.com/oauth2/token";
        private const string DEVICE_URL = "https://api.fitbit.com/1/user/-/devices.json";
        private const string HEARTRATE_URL = "https://api.fitbit.com/1/user/-/activities/heart/date/{0}/1d.json";

        internal static List<HeartRateDayData> GetHeartrateForDay(DateTimeOffset day)
        {
            Tuple<HeartData, bool> result = GetDataFromFitbit<HeartData>(String.Format(HEARTRATE_URL, day.ToString(Settings.FITBIT_FORMAT_DAY)));
            HeartData heartrateData = result.Item1;
            bool retry = result.Item2;

            if (heartrateData == null && retry)
            {
                heartrateData = GetDataFromFitbit<HeartData>(String.Format(HEARTRATE_URL, day.Year + "-" + day.Month + "-" + day.Day)).Item1;
            }
            
            List<HeartRateDayData> data = new List<HeartRateDayData>();

            if (heartrateData.Activities.Count > 0)
            {

                foreach (HeartRateZone zone in heartrateData.Activities[0].Value.CustomHeartrateZones)
                {
                    data.Add(new HeartRateDayData {
                        Date = DateTime.ParseExact(heartrateData.Activities[0].DateTime, Settings.FITBIT_FORMAT_DAY, CultureInfo.InvariantCulture),
                        RestingHeartrate = heartrateData.Activities[0].Value.RestingHeartrate,
                        CaloriesOut = zone.CaloriesOut,
                        Max = zone.Max,
                        Min = zone.Min,
                        MinutesSpent = zone.Minutes,
                        Name = zone.Name
                    });
                }

                foreach (HeartRateZone zone in heartrateData.Activities[0].Value.HeartRateZones)
                {
                    data.Add(new HeartRateDayData
                    {
                        Date = DateTime.ParseExact(heartrateData.Activities[0].DateTime, Settings.FITBIT_FORMAT_DAY, CultureInfo.InvariantCulture),
                        RestingHeartrate = heartrateData.Activities[0].Value.RestingHeartrate,
                        CaloriesOut = zone.CaloriesOut,
                        Max = zone.Max,
                        Min = zone.Min,
                        MinutesSpent = zone.Minutes,
                        Name = zone.Name
                    });
                }
            }

            return data;
        }

        internal static SleepData GetSleepDataForDay(DateTimeOffset day)
        {
            Tuple<SleepData, bool> result = GetDataFromFitbit<SleepData>(String.Format(SLEEP_URL, day.Year + "-" + day.Month + "-" + day.Day));
            SleepData sleepData = result.Item1;
            bool retry = result.Item2;

            if (sleepData == null && retry)
            {
                sleepData = GetDataFromFitbit<SleepData>(String.Format(SLEEP_URL, day.Year + "-" + day.Month + "-" + day.Day)).Item1;
            }

            return sleepData;
        }

        internal static List<Device> GetDeviceData()
        {
            Tuple<List<Device>, bool> result = GetDataFromFitbit<List<Device>>(DEVICE_URL);
            List<Device> devices = result.Item1;
            bool retry = result.Item2;

            if (devices == null && retry)
            {
                devices = GetDataFromFitbit<List<Device>>(DEVICE_URL).Item1;
            }

            return devices;
        }

        private static Tuple<T, bool> GetDataFromFitbit<T>(string url)
        {
            WebClient client = null;
            Stream data = null;
            StreamReader reader = null;

            try
            {
                client = new WebClient();
                client.Headers.Add("Authorization", "Bearer " + Database.GetInstance().GetSettingsString(Settings.ACCESS_TOKEN, null));

                data = client.OpenRead(url);

                reader = new StreamReader(data);
                string response = reader.ReadToEnd();
             
                T dataObject = JsonConvert.DeserializeObject<T>(response);
                return Tuple.Create<T, bool>(dataObject, false);
            }
            catch (WebException e)
            {
                if ((e.Response is HttpWebResponse) && (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                {
                    RefreshAccessToken();
                    return Tuple.Create<T, bool>(default(T), true);
                }
                else if ((e.Response is HttpWebResponse) && (e.Response as HttpWebResponse).StatusCode.ToString().Equals("429"))
                {
                    Console.WriteLine("Too many requests");
                    return Tuple.Create<T, bool>(default(T), false);
                }
                else
                {
                    Console.WriteLine((e.Response as HttpWebResponse).StatusCode);
                    return Tuple.Create<T, bool>(default(T), false);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return Tuple.Create<T, bool>(default(T), false);
            }
            finally
            {
                if (data != null)
                {
                    data.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        internal static DateTimeOffset GetLatestSyncDate()
        {
            List<Device> devices = GetDeviceData();
            if (devices == null)
            {
                return DateTimeOffset.MinValue;
            }
            List<DateTimeOffset> syncTimes = new List<DateTimeOffset>();
            foreach (Device device in devices)
            {
                if (device.Type.Equals("TRACKER"))
                {
                    syncTimes.Add(device.LastSyncTime);
                }
            }
            return syncTimes.Min();
        }

        internal static void RefreshAccessToken()
        {
            Console.WriteLine("Access token not valid anymore. Try to refresh access token.");

            WebClient client = new WebClient();
            string accessToken = Database.GetInstance().GetSettingsString(Settings.CLIENT_ID, null) + ":" + Database.GetInstance().GetSettingsString(Settings.CLIENT_SECRET, null);
            accessToken = Base64Encode(accessToken);
            client.Headers.Add("Authorization", "Basic " + accessToken);
            Console.WriteLine(accessToken);

            var values = new NameValueCollection();
            values["grant_type"] = "refresh_token";
            string refreshToken = Database.GetInstance().GetSettingsString(Settings.REFRESH_TOKEN, null);
            Console.WriteLine(refreshToken);
            values["refresh_token"] = refreshToken;
            values["expires_in"] = "" + Settings.TOKEN_LIFETIME;

            var response = client.UploadValues(REFRESH_URL, values);
            var responseString = Encoding.Default.GetString(response);
            AccessRefreshResponse accessResponse = JsonConvert.DeserializeObject<AccessRefreshResponse>(responseString);
            Console.WriteLine("Refreshing token returned the following response: " + responseString);
            Console.WriteLine("Writing access and refresh token to database.");

            Database.GetInstance().LogInfo("Retreived new access and refresh token: " + accessResponse.access_token + " / " + accessResponse.refresh_token);
            Database.GetInstance().SetSettings(Settings.ACCESS_TOKEN, accessResponse.access_token);
            Database.GetInstance().SetSettings(Settings.REFRESH_TOKEN, accessResponse.refresh_token);
            client.Dispose();
        }

        internal static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }

}