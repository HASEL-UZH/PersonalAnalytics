// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using System.Linq;
using Windows.Security.Credentials;

namespace FitbitTracker.Data
{

    //Stores access token. See: https://social.msdn.microsoft.com/Forums/en-US/8160064a-dd96-463f-b3b7-4243f20c13e4/recommended-way-to-store-oauth-clientid-and-secret-for-rest-services-in-a-xaml-metro-app?forum=winappswithcsharp
    public class SecretStorage
    {
        private const string RESOURCE_NAME = "OAuth_Token";
        private const string ACCESS_TOKEN = "accessToken";
        private const string REFRESH_TOKEN = "refreshToken";

        private const string FITBIT_CREDENTIALS = "FitbitCredentials";
        private const string FITBIT_CLIENT_ID = "FitbitClientID";
        private const string FITBIT_CLIENT_SECRET = "FitbitClientSecret";
        private const string FITBIT_FIRST_AUTHORIZATION_CODE = "FitbitFirstAuthorizationCode";

        public static void SaveFitbitClientID(string clientID)
        {
            var vault = new PasswordVault();
            var credentials = new PasswordCredential(FITBIT_CREDENTIALS, FITBIT_CLIENT_ID, clientID);
            vault.Add(credentials);
        }

        public static void SaveFitbitClientSecret(string clientSecret)
        {
            var vault = new PasswordVault();
            var credentials = new PasswordCredential(FITBIT_CREDENTIALS, FITBIT_CLIENT_SECRET, clientSecret);
            vault.Add(credentials);
        }

        public static void SaveFitbitFirstAuthorizationCode(string firstAuthorizationCode)
        {
            var vault = new PasswordVault();
            var credentials = new PasswordCredential(FITBIT_CREDENTIALS, FITBIT_FIRST_AUTHORIZATION_CODE, firstAuthorizationCode);
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

        public static void RemoveRefreshToken(string refreshToken)
        {
            var vault = new PasswordVault();
            vault.Remove(new PasswordCredential(RESOURCE_NAME, REFRESH_TOKEN, refreshToken));
        }

        public static void SaveRefreshToken(string refreshToken)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(RESOURCE_NAME, REFRESH_TOKEN, refreshToken);
            vault.Add(credential);
        }

        public static string GetFitbitClientSecret()
        {
            return GetFitbitCredentials(FITBIT_CLIENT_SECRET);
        }

        public static string GetFitbitClientID()
        {
            return GetFitbitCredentials(FITBIT_CLIENT_ID);
        }

        public static string GetFibitFirstAuthorizationCode()
        {
            return GetFitbitCredentials(FITBIT_FIRST_AUTHORIZATION_CODE);
        }

        public static string GetAccessToken()
        {
            return GetToken(ACCESS_TOKEN);
        }

        public static string GetRefreshToken()
        {
            return GetToken(REFRESH_TOKEN);
        }

        private static string GetFitbitCredentials(string kind)
        {
            var vault = new PasswordVault();
            try
            {
                var credential = vault.FindAllByResource(FITBIT_CREDENTIALS).FirstOrDefault();
                if (credential != null)
                {
                    return vault.Retrieve(FITBIT_CREDENTIALS, kind).Password;
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
