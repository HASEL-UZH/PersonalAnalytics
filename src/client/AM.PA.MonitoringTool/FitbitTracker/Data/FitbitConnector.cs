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
        SLEEP, ACTIVITIES, HR, STEPS
    };

    public class FitbitConnector
    {

        //URls of the web API
        private const string SLEEP_URL = "https://api.fitbit.com/1/user/-/sleep/date/{0}.json";
        private const string REFRESH_URL = "https://api.fitbit.com/oauth2/token";
        private const string DEVICE_URL = "https://api.fitbit.com/1/user/-/devices.json";
        private const string HEARTRATE_URL = "https://api.fitbit.com/1/user/-/activities/heart/date/{0}/1d/1sec.json";
        private const string ACTIVITY_URL = "https://api.fitbit.com/1/user/-/activities/date/{0}.json";
        private const string STEP_URL = "https://api.fitbit.com/1/user/-/activities/steps/date/{0}/1d/1min.json";
        private const string STEP_AGGREGATED_URL = "https://api.fitbit.com/1/user/-/activities/steps/date/{0}/1d/15min.json";
        private const string REVOKE_URL = "https://api.fitbit.com/oauth2/revoke";

        //Called when refreshing the access token fails
        public delegate void OnRefreshTokenFail();
        public static event OnRefreshTokenFail RefreshTokenFail;

        internal static StepData GetStepDataAggregatedForDay(DateTimeOffset day)
        {
            Tuple<StepData, bool> result = GetDataFromFitbit<StepData>(String.Format(STEP_AGGREGATED_URL, day.ToString(Settings.FITBIT_FORMAT_DAY)));
            StepData stepData = result.Item1;
            bool retry = result.Item2;

            if (stepData == null && retry)
            {
                stepData = GetDataFromFitbit<StepData>(String.Format(STEP_AGGREGATED_URL, day.ToString(Settings.FITBIT_FORMAT_DAY))).Item1;
            }

            return stepData;
        }

        internal static StepData GetStepDataForDay(DateTimeOffset day)
        {
            Tuple<StepData, bool> result = GetDataFromFitbit<StepData>(String.Format(STEP_URL, day.ToString(Settings.FITBIT_FORMAT_DAY)));
            StepData stepData = result.Item1;
            bool retry = result.Item2;

            if (stepData == null && retry)
            {
                stepData = GetDataFromFitbit<StepData>(String.Format(STEP_URL, day.ToString(Settings.FITBIT_FORMAT_DAY))).Item1;
            }

            return stepData;
        }

        internal static ActivityData GetActivityDataForDay(DateTimeOffset day)
        {
            Tuple<ActivityData, bool> result = GetDataFromFitbit< ActivityData>(String.Format(ACTIVITY_URL, day.ToString(Settings.FITBIT_FORMAT_DAY)));
            ActivityData activityData = result.Item1;
            bool retry = result.Item2;

            if (activityData == null && retry)
            {
                activityData = GetDataFromFitbit<ActivityData>(String.Format(ACTIVITY_URL, day.ToString(Settings.FITBIT_FORMAT_DAY))).Item1;
            }

            return activityData;
        }

        internal static Tuple<List<HeartRateDayData>, List<HeartrateIntraDayData>> GetHeartrateForDay(DateTimeOffset day)
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

            List<HeartrateIntraDayData> intradayData = new List<HeartrateIntraDayData>();

            foreach (HeartrateIntradayData d in result.Item1.IntradayActivities.HeartrateIntradayData)
            {
                intradayData.Add(new HeartrateIntraDayData
                {
                    Day = new DateTime(day.Year, day.Month, day.Day),
                    Time = d.Time,
                    Value = d.Value
                });
            }

            return Tuple.Create<List<HeartRateDayData>, List<HeartrateIntraDayData>>(data, intradayData);
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

        //Generic method that retrieves specific data from the fitbit. If an exception is thrown during this process, it checks whether the problem is an authorization problem. In this case, the tokens are refreshed.
        //The method returns a tuple, consisting of two values. The first item in the tuple is the retrieved data set, or the default value in case an exception was thrown and the second item, indicates whether a caller
        //of this method should retry to call this method in case of an exception.
        private static Tuple<T, bool> GetDataFromFitbit<T>(string url)
        {
            WebClient client = null;
            Stream data = null;
            StreamReader reader = null;

            try
            {
                client = new WebClient();
                client.Headers.Add("Authorization", "Bearer " + SecretStorage.GetAccessToken());

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
                    Logger.WriteToConsole("Too many requests");
                    return Tuple.Create<T, bool>(default(T), false);
                }
                else
                {
                    Logger.WriteToLogFile(e);
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

        //Returns the latest point in time a tracker was synchronized with fitbit
        internal static DateTimeOffset GetLatestSyncDate()
        {
            List<Device> devices = GetDeviceData();
            if (devices == null || devices.Count == 0)
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

        internal static void GetFirstAccessToken(string registrationToken)
        {
            Logger.WriteToConsole("Try to get first access token");

            WebClient client = new WebClient();
            client.Headers.Add("Authorization", "Basic " + Settings.FIRST_AUTHORIZATION_CODE);

            var values = new NameValueCollection();
            values["clientId"] = Settings.CLIENT_ID;
            values["grant_type"] = "authorization_code";
            values["redirect_uri"] = Settings.REDIRECT_URI;
            values["code"] = registrationToken;

            var response = client.UploadValues(REFRESH_URL, values);
            var responseString = Encoding.Default.GetString(response);
            AccessRefreshResponse accessResponse = JsonConvert.DeserializeObject<AccessRefreshResponse>(responseString);

            Database.GetInstance().LogInfo("Retreived new access and refresh token: " + accessResponse.access_token + " / " + accessResponse.refresh_token);
            SecretStorage.SaveAccessToken(accessResponse.access_token);
            SecretStorage.SaveRefreshToken(accessResponse.refresh_token);

            client.Dispose();
        }

        public static void RevokeAccessToken(string tokenToBeRevoked)
        {
            WebClient client = new WebClient();
            string accessToken = Settings.CLIENT_ID + ":" + Settings.CLIENT_SECRET;
            accessToken = Base64Encode(accessToken);
            client.Headers.Add("Authorization", "Basic " + accessToken);

            var values = new NameValueCollection();
            values["token"] = tokenToBeRevoked;

            try
            {
                var response = client.UploadValues(REVOKE_URL, values);
                var responseString = Encoding.Default.GetString(response);

                SecretStorage.RemoveAccessToken(tokenToBeRevoked);
                SecretStorage.RemoveRefreshToken(SecretStorage.GetRefreshToken());
            }
            catch (WebException e)
            {
                if ((e.Response is HttpWebResponse) && ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized || (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest))
                {
                    RefreshTokenFail?.Invoke();
                }
                else if ((e.Response is HttpWebResponse) && (e.Response as HttpWebResponse).StatusCode.ToString().Equals("429"))
                {
                    Logger.WriteToConsole("Too many requests");
                }
                else
                {
                    Logger.WriteToLogFile(e);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                Logger.WriteToConsole(e.ToString());
            }
            finally
            {
                client.Dispose();
            }
        }

        internal static void RefreshAccessToken()
        {
            Logger.WriteToConsole("Access token not valid anymore. Try to refresh access token.");

            WebClient client = new WebClient();
            string accessToken = Settings.CLIENT_ID + ":" + Settings.CLIENT_SECRET;
            accessToken = Base64Encode(accessToken);
            client.Headers.Add("Authorization", "Basic " + accessToken);
            
            var values = new NameValueCollection();
            values["grant_type"] = "refresh_token";
            string refreshToken = SecretStorage.GetRefreshToken();
            values["refresh_token"] = refreshToken;
            values["expires_in"] = "" + Settings.TOKEN_LIFETIME;

            try
            {
                var response = client.UploadValues(REFRESH_URL, values);
                var responseString = Encoding.Default.GetString(response);
                AccessRefreshResponse accessResponse = JsonConvert.DeserializeObject<AccessRefreshResponse>(responseString);
                Logger.WriteToConsole("Refreshing token returned the following response: " + responseString);
                Logger.WriteToConsole("Writing access and refresh token to database.");

                Database.GetInstance().LogInfo("Retreived new access and refresh token: " + accessResponse.access_token + " / " + accessResponse.refresh_token);
                SecretStorage.SaveAccessToken(accessResponse.access_token);
                SecretStorage.SaveRefreshToken(accessResponse.refresh_token);
            }
            catch (WebException e)
            {
                if ((e.Response is HttpWebResponse) && ( (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized || (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest))
                {
                    RefreshTokenFail?.Invoke();
                }
                else if ((e.Response is HttpWebResponse) && (e.Response as HttpWebResponse).StatusCode.ToString().Equals("429"))
                {
                    Logger.WriteToConsole("Too many requests");
                }
                else
                {
                    Logger.WriteToLogFile(e);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                Logger.WriteToConsole(e.ToString());
            }
            finally
            {
                client.Dispose();
            }
        }

        internal static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }

}