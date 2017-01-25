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

        internal static DateTimeOffset GetLatestSyncDate()
        {
            List<Device> devices = GetDeviceData();
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
        
        internal static List<Device> GetDeviceData()
        {
            WebClient client = new WebClient();
            client.Headers.Add("Authorization", "Bearer " + Database.GetInstance().GetSettingsString(Settings.ACCESS_TOKEN, null));

            Stream data = client.OpenRead(DEVICE_URL);
            StreamReader reader = new StreamReader(data);
            string response = reader.ReadToEnd();
            response = response.Replace(@"\", "");
            
            List<Device> device = JsonConvert.DeserializeObject<List<Device>>(response);
            return device;
        }

        private static T GetDataFromFitbit<T>(Type clazz, string url)
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
                return dataObject;
            }
            catch (WebException e)
            {
                if ((e.Response is HttpWebResponse) && (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                {
                    RefreshAccessToken();
                    return default(T);
                }
                else
                {
                    Console.WriteLine((e.Response as HttpWebResponse).StatusCode);
                    return default(T);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return default(T);
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

        internal static SleepData GetSleepDataForDay(DateTimeOffset day)
        {
            WebClient client = null;
            Stream data = null;
            StreamReader reader = null;

            try
            {
                client = new WebClient();
                client.Headers.Add("Authorization", "Bearer " + Database.GetInstance().GetSettingsString(Settings.ACCESS_TOKEN, null));

                data = client.OpenRead(String.Format(SLEEP_URL, day.Year + "-" + day.Month + "-" + day.Day));

                reader = new StreamReader(data);
                string response = reader.ReadToEnd();

                SleepData dataObject = JsonConvert.DeserializeObject<SleepData>(response);
                return dataObject; 
            }
            catch (Exception e)
            {
                //TODO: Only if status code = 401
                if (e is WebException)
                {
                    RefreshAccessToken();
                    return GetSleepDataForDay(day);
                }
                else
                {
                    Logger.WriteToLogFile(e);
                    return null;
                }
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

        internal static void RefreshAccessToken()
        {
            Console.WriteLine("Access token not valid anymore. Try to refresh access token.");

            WebClient client = new WebClient();
            string accessToken = Database.GetInstance().GetSettingsString(Settings.ACCESS_TOKEN, null);
            client.Headers.Add("Authorization", "Basic " + accessToken);
            Console.WriteLine(accessToken);

            var values = new NameValueCollection();
            values["grant_type"] = "refresh_token";
            string refreshToken = Database.GetInstance().GetSettingsString(Settings.REFRESH_TOKEN, null);
            Console.WriteLine(refreshToken);
            values["refresh_token"] = refreshToken;

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

    }

}