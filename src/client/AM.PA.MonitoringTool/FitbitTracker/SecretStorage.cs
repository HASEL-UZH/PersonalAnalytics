// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace FitbitTracker
{

    //Stores access token. See: https://social.msdn.microsoft.com/Forums/en-US/8160064a-dd96-463f-b3b7-4243f20c13e4/recommended-way-to-store-oauth-clientid-and-secret-for-rest-services-in-a-xaml-metro-app?forum=winappswithcsharp
    public class SecretStorage
    {
        private const string RESOURCE_NAME = "OAuth_Token";
        private const string ACCESS_TOKEN = "accessToken";
        private const string REFRESH_TOKEN = "refreshToken";

        public static void SaveAccessToken(string accessToken)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(RESOURCE_NAME, ACCESS_TOKEN, accessToken);
            vault.Add(credential);
        }

        public static void SaveRefreshToken(string refreshToken)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(RESOURCE_NAME, REFRESH_TOKEN, refreshToken);
            vault.Add(credential);
        }

        public static string GetAccessToken()
        {
            return GetToken(ACCESS_TOKEN);
        }

        public static string GetRefreshToken()
        {
            return GetToken(REFRESH_TOKEN);
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
