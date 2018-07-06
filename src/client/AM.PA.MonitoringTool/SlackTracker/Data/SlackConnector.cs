// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at University of Zurich
// Created: 2018-07-06
// 
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Shared.Data;
using System.Collections.Specialized;
using System.Text;
using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using FitbitTracker.Data;

namespace SlackTracker.Data
{
    class SlackConnector
    {

        //URls of the web API
        private const string REFRESH_URL = "https://slack.com/api/oauth.access";
        private const string REVOKE_URL = "https://slack.com/api/auth.revoke";

        //Called when refreshing the access token fails
        public delegate void OnRefreshTokenFail();
        public static event OnRefreshTokenFail RefreshTokenFail;

        //Called when token access is revoked
        public delegate void OnTokenAccessRevoked();
        public static event OnTokenAccessRevoked TokenRevoked;

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
                AccessRefreshResponse accessResponse = JsonConvert.DeserializeObject<AccessRefreshResponse>(responseString);

                Database.GetInstance().LogInfo("Retreived new access and refresh token: " + accessResponse.access_token + " / " + accessResponse.refresh_token);
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
    }
}