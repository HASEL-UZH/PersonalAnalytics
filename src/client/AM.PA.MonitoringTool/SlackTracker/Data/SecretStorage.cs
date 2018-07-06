// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using System.Linq;
using Windows.Security.Credentials;

namespace SlackTracker.Data
{

    //Stores access token. See: https://social.msdn.microsoft.com/Forums/en-US/8160064a-dd96-463f-b3b7-4243f20c13e4/recommended-way-to-store-oauth-clientid-and-secret-for-rest-services-in-a-xaml-metro-app?forum=winappswithcsharp
    public class SecretStorage
    {
        private const string RESOURCE_NAME = "Slack_OAuth_Token";
        private const string ACCESS_TOKEN = "slack_accessToken";
        private const string REFRESH_TOKEN = "slack_refreshToken";

        private const string SLACK_CREDENTIALS = "SlackCredentials";
        private const string SLACK_CLIENT_ID = "SlackClientID";
        private const string SLACK_CLIENT_SECRET = "SlackClientSecret";
        private const string SLACK_FIRST_AUTHORIZATION_CODE = "SlackFirstAuthorizationCode";

        public static void SaveSlackClientID(string clientID)
        {
            var vault = new PasswordVault();
            var credentials = new PasswordCredential(SLACK_CREDENTIALS, SLACK_CLIENT_ID, clientID);
            vault.Add(credentials);
        }

        public static void SaveSlackClientSecret(string clientSecret)
        {
            var vault = new PasswordVault();
            var credentials = new PasswordCredential(SLACK_CREDENTIALS, SLACK_CLIENT_SECRET, clientSecret);
            vault.Add(credentials);
        }

        public static void SaveAccessToken(string accessToken)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(RESOURCE_NAME, ACCESS_TOKEN, accessToken);
            vault.Add(credential);
        }

        public static void RemoveAccessToken(string accessToken)
        {
            var vault = new PasswordVault();
            vault.Remove(new PasswordCredential(RESOURCE_NAME, ACCESS_TOKEN, accessToken));
        }

        public static string GetSlackClientSecret()
        {
            return GetSlackCredentials(SLACK_CLIENT_SECRET);
        }

        public static string GetSlackClientID()
        {
            return GetSlackCredentials(SLACK_CLIENT_ID);
        }

        public static string GetAccessToken()
        {
            return GetToken(ACCESS_TOKEN);
        }

        private static string GetSlackCredentials(string kind)
        {
            var vault = new PasswordVault();
            try
            {
                var credential = vault.FindAllByResource(SLACK_CREDENTIALS).FirstOrDefault();
                if (credential != null)
                {
                    return vault.Retrieve(SLACK_CREDENTIALS, kind).Password;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static string GetToken(string kind)
        {
            var vault = new PasswordVault();
            try
            {
                var credential = vault.FindAllByResource(RESOURCE_NAME).FirstOrDefault();
                
                if (credential != null)
                {
                    return vault.Retrieve(RESOURCE_NAME, kind).Password;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}
