// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

using System;
using System.Net;
using System.Text;
using Shared;
using Shared.Data;
using Shared.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Specialized;
using SlackTracker.Data.SlackModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace SlackTracker.Data
{
    public enum AnalysisType
    {
        THREAD, USER_ACTIVITY
    };

    class SlackConnector
    {
        private delegate T ResponseParser<T>(string response);

        //URls of the web API
        private const string REFRESH_URL = "https://slack.com/api/oauth.access";
        private const string REVOKE_URL = "https://slack.com/api/auth.revoke";
        private const string CHANNELS_LIST_URL = "https://slack.com/api/channels.list";
        private const string CHANNELS_HISTORY_URL = "https://slack.com/api/channels.history";
        private const string USERS_LIST_URL = "https://slack.com/api/users.list";

        //Called when token access is revoked
        public delegate void OnTokenAccessRevoked();
        public static event OnTokenAccessRevoked TokenRevoked;


        #region Fetch Logs and Update

        public static List<LogData> GetLogs(DateTimeOffset latestSync)
        {
            List<Log> channel_logs = new List<Log>();
            List<Channel> _channels = DatabaseConnector.GetChannels();

            Logger.WriteToConsole("Fetching Logs");

            foreach (Channel c in _channels)
            {
                DateTimeOffset oldest = latestSync;
                bool has_more = true;

                while (has_more)
                {
                    var values = new NameValueCollection();
                    values["token"] = SecretStorage.GetAccessToken();
                    values["channel"] = c.Id;
                    values["count"] = "1000";
                    if (oldest != DateTimeOffset.MinValue) { values["oldest"] = DateTimeHelper.JavascriptTimestampFromDateTime(oldest.DateTime).ToString(); }

                    Tuple<LogResponse, bool> result = GetDataFromSlack<LogResponse>(CHANNELS_HISTORY_URL, values, ParseLogResponse);

                    if (result.Item1 == null && result.Item2)
                    {
                        result = GetDataFromSlack<LogResponse>(CHANNELS_HISTORY_URL, values, ParseLogResponse);
                    }
                    List<Log> logData = result.Item1.Log;
                    has_more = result.Item1.HasMore;
                    oldest = result.Item1.LastTimestamp;

                    // Update channel_id property
                    logData.ForEach(m => m.ChannelId = c.Id);

                    //Reverse so that message are in ordered by send time
                    logData.Reverse();

                    channel_logs.AddRange(logData);
                }
            }

            return ConvertTempLog(channel_logs);
        }

        private static LogResponse ParseLogResponse(string response)
        {
            LogResponse ret = new LogResponse();
            JObject response_object = JObject.Parse(response);
            JArray messages;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            messages = (JArray)response_object["messages"];
            ret.HasMore = (bool)response_object["has_more"];

            List<Log> _logs = messages.Select(p => new Log
            {
                Sender = (string)p["user"],
                Message = (string)p["text"],
                Timestamp = (string)p["ts"]
            }).ToList();

            _logs = _logs.Where(x => !(x.Message.Contains("has joined the channel") || x.Message.Contains("has left the channel"))).ToList();
            ret.Log = _logs;
            if (ret.HasMore) { ret.LastTimestamp = DateTimeHelper.DateTimeFromSlackTimestamp(_logs.Min(l => l.Timestamp).Split('.')[0]); }
            else { ret.LastTimestamp = DateTimeOffset.Now; }
            return ret;
        }

        public static List<Channel> GetChannels()
        {
            var values = new NameValueCollection();
            values["token"] = SecretStorage.GetAccessToken();
            values["exclude_archived"] = "true";
            values["exclude_members"] = "true";

            Tuple<List<Channel>, bool> result = GetDataFromSlack<List<Channel>>(CHANNELS_LIST_URL, values, ParseChannelList);
            bool retry = result.Item2;

            if (result.Item1 == null && retry)
            {
                result = GetDataFromSlack<List<Channel>>(CHANNELS_LIST_URL, values, ParseChannelList);
            }

            return result.Item1;
        }

        private static List<Channel> ParseChannelList(string response)
        {
            JObject response_object = JObject.Parse(response);
            JArray channels;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            channels = (JArray)response_object["channels"];

            List<Channel> _channels = channels.Select(p => new Channel
            {
                Id = (string)p["id"],
                Name = (string)p["name"],
                Created = (long)p["created"],
                Creator = (string)p["creator"]
            }).ToList();

            return _channels;
        }

        public static List<User> GetUsers()
        {
            var values = new NameValueCollection();
            values["token"] = SecretStorage.GetAccessToken();
            values["include_locale"] = "true";

            Tuple<List<User>, bool> result = GetDataFromSlack<List<User>>(USERS_LIST_URL, values, ParseUserList);
            bool retry = result.Item2;

            if (result.Item1 == null && retry)
            {
                result = GetDataFromSlack<List<User>>(USERS_LIST_URL, values, ParseUserList);
            }

            return result.Item1;
        }

        private static List<User> ParseUserList(string response)
        {
            JObject response_object = JObject.Parse(response);
            JArray members;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            members = (JArray)response_object["members"];


            List<User> _users = members.Select(p => new User
            {
                Id = (string)p["id"],
                TeamId = (string)p["team_id"],
                Name = (string)p["name"],
                RealName = (string)p["profile"]["real_name"],
                IsBot = (bool)p["is_bot"]
            }).ToList();

            return _users;
        }

        //Generic method that retrieves specific data from the slack. If an exception is thrown during this process, it checks whether the problem is an authorization problem. In this case, the tokens are refreshed.
        //The method returns a tuple, consisting of two values. The first item in the tuple is the retrieved data set, or the default value in case an exception was thrown and the second item, indicates whether a caller
        //of this method should retry to call this method in case of an exception.
        private static Tuple<T, bool> GetDataFromSlack<T>(string url, NameValueCollection values, ResponseParser<T> parser)
        {
            WebClient client = null;

            try
            {
                client = new WebClient();

                var response = client.UploadValues(url, values);
                var responseString = Encoding.Default.GetString(response);

                T dataObject = parser(responseString);
                return Tuple.Create<T, bool>(dataObject, false);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return Tuple.Create<T, bool>(default(T), true);
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        #endregion

        #region authorization and tokens

        public static void RevokeAccessToken(string tokenToBeRevoked)
        {
            TokenRevoked?.Invoke();

            WebClient client = new WebClient();
            string accessToken = SecretStorage.GetSlackClientID() + ":" + SecretStorage.GetSlackClientSecret();
            accessToken = Base64Encode(accessToken);
            client.Headers.Add("Authorization", "Basic " + accessToken);

            var values = new NameValueCollection();
            values["token"] = tokenToBeRevoked;

            try
            {
                var response = client.UploadValues(REVOKE_URL, values);
                var responseString = Encoding.Default.GetString(response);

                SecretStorage.RemoveAccessToken(tokenToBeRevoked);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                Logger.WriteToConsole(e.ToString());
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        internal static void GetAccessToken(string registrationToken)
        {
            WebClient client = new WebClient();

            try
            {
                Logger.WriteToConsole("Try to get first access token");

                client.Headers.Add("Authorization", "Basic ");

                var values = new NameValueCollection();
                values["client_id"] = SecretStorage.GetSlackClientID();
                values["client_secret"] = SecretStorage.GetSlackClientSecret();
                values["redirect_uri"] = Settings.REDIRECT_URI;
                values["code"] = registrationToken;

                var response = client.UploadValues(REFRESH_URL, values);
                var responseString = Encoding.Default.GetString(response);

                AccessResponse accessResponse = JsonConvert.DeserializeObject<AccessResponse>(responseString);

                Database.GetInstance().LogInfo("Retreived new access" + accessResponse.AccessToken);
                SecretStorage.SaveAccessToken(accessResponse.AccessToken);

                client.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        internal static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        #endregion

        #region Helpers
        private static List<LogData> ConvertTempLog(List<Log> logs)
        {
            List<LogData> logData = new List<LogData>();
            int n_log = 1;

            foreach (Log log in logs)
            {
                logData.Add(new LogData
                {
                    Id = n_log++,
                    Timestamp = DateTimeHelper.DateTimeFromSlackTimestamp(log.Timestamp),
                    ChannelId = log.ChannelId,
                    Author = log.Sender,
                    Mentions = GetUserMentions(log.Message),
                    Message = log.Message
                });
            }

            return logData;
        }

        // Receivers occurs in message enclosed in <> and starting with a @ i.e <@C310FAE>
        private static List<string> GetUserMentions(string text)
        {
            List<string> mentions = new List<string>();
            string pattern = @"<@[A-Z0-9]+>";
            foreach (Match m in Regex.Matches(text, pattern))
            {
                string mention = m.Value;
                int len = mention.Length;

                //remove <@>
                mentions.Add(mention.Substring(2, len - 3));
            }

            return mentions;
        }
        #endregion
    }

    internal class Log
    {
        public string Timestamp { get; set; }
        public string ChannelId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    internal class LogResponse
    {
        public List<Log> Log { get; set; }
        public bool HasMore { get; set; }
        public DateTimeOffset LastTimestamp { get; set; }
    }
}