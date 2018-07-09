// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using Shared.Data;
using System.Collections.Specialized;
using System.Text;
using Shared;
using Newtonsoft.Json;
using System.Collections.Generic;
using SlackTracker.Data.SlackModel;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace SlackTracker.Data
{
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


        //Returns the latest point in time a tracker was synchronized with slack
        internal static DateTimeOffset GetLatestSyncDate()
        {
            return DateTimeOffset.MinValue;

            //TODO: Implement
        }

        #region Fetch Logs and Update
        public static Dictionary<string, IList<Log>> Get_Logs(IList<Channel> _channels, DateTimeOffset _last_fetched)
        {
            Dictionary<string, IList<Log>> channel_logs = new Dictionary<string, IList<Log>>;

            foreach (Channel c in _channels)
            {
                var values = new NameValueCollection();
                values["token"] = SecretStorage.GetAccessToken();
                values["channel"] = c.id;
                //values["oldest"] = _last_fetched;

                Tuple<IList<Log>, bool> result = GetDataFromSlack<IList<Log>>(CHANNELS_HISTORY_URL, values, parse_log_response);

                IList<Log> logData = result.Item1;

                channel_logs[c.id] = logData;
            }



            return channel_logs;
        }

        private static IList<Log> parse_log_response (string response)
        {
            JObject response_object = JObject.Parse(response);
            JArray messages;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            messages = (JArray)response_object["messages"];


            IList<Log> _logs = messages.Select(p => new Log
            {
                type = (string)p["type"],
                sender = (string)p["user"],
                message = (string)p["text"],
                timestamp = (string)p["ts"]
            }).ToList();

            return _logs;
        }

        public static IList<Channel> GetChannels()
        {
            var values = new NameValueCollection();
            values["token"] = SecretStorage.GetAccessToken();
            values["exclude_archived"] = "true";
            values["exclude_members"] = "true";
            
            Tuple<IList<Channel>, bool> result = GetDataFromSlack<IList<Channel>>(CHANNELS_LIST_URL, values, parse_channel_list);

            return result.Item1;
        }

        private static IList<Channel> parse_channel_list (string response)
        {   
            JObject response_object = JObject.Parse(response);
            JArray channels;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            channels = (JArray) response_object["channels"];


            IList<Channel> _channels = channels.Select(p => new Channel
            {
                id = (string)p["id"],
                name = (string)p["name"],
                created = (int)p["created"],
                creator = (string)p["creator"]
            }).ToList();

            return _channels;
        }

        private static IList<User> GetUsers()
        {
            var values = new NameValueCollection();
            values["token"] = SecretStorage.GetAccessToken();
            values["include_locale"] = "true";

            Tuple<IList<User>, bool> result = GetDataFromSlack<IList<User>>(USERS_LIST_URL, values, parse_user_list);

            return result.Item1;
        }

        private static IList<User> parse_user_list (string response)
        {
            JObject response_object = JObject.Parse(response);
            JArray members;
            bool status;

            status = (bool)response_object.SelectToken("ok");

            if (!status)
                return null;

            members = (JArray)response_object["members"];


            IList<User> _users = members.Select(p => new User
            {
                id = (string)p["id"],
                team_id = (string)p["team_id"],
                name = (string)p["name"],
                real_name = (string)p["real_name"],
                is_bot = (bool)p["is_bot"],
                is_admin = (bool)p["is_admin"],
                is_owner = (bool)p["is_owner"]
            }).ToList();

            return _users;
        }

        //Generic method that retrieves specific data from the fitbit. If an exception is thrown during this process, it checks whether the problem is an authorization problem. In this case, the tokens are refreshed.
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

                Logger.WriteToConsole("Response: " + responseString);

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
                Logger.WriteToConsole(responseString);
                AccessResponse accessResponse = JsonConvert.DeserializeObject<AccessResponse>(responseString);

                Database.GetInstance().LogInfo("Retreived new access" + accessResponse.access_token);
                SecretStorage.SaveAccessToken(accessResponse.access_token);

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

        private static string get_receiver (string text)
        {
            return "not implemented";
        }
        #endregion
    }
}