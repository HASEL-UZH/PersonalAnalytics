//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.IO;
using Microsoft.Identity.Client;

namespace MsOfficeTracker.Helpers
{

    /// <summary>
    /// This is a simple persistent cache implementation for a desktop application.
    /// It uses DPAPI for storing tokens in a local file.
    /// 
    /// from: https://raw.githubusercontent.com/Azure-Samples/active-directory-dotnet-desktop-msgraph-v2/master/active-directory-wpf-msgraph-v2/TokenCacheHelper.cs
    /// from: https://github.com/Azure-Samples/active-directory-dotnet-native-aspnet5/blob/master/TodoListClient/FileCache.cs
    /// </summary>
    internal static class FileCache
    {
        private static readonly object FileLock = new object();
        public static TokenCache UsertokenCache;

        /// <summary>
        /// Get the user token cache
        /// </summary>
        /// <returns></returns>
        public static TokenCache GetUserCache()
        {
            if (UsertokenCache == null)
            {
                UsertokenCache = new TokenCache();
                UsertokenCache.SetBeforeAccess(BeforeAccessNotification);
                UsertokenCache.SetAfterAccess(AfterAccessNotification);
            }
            return UsertokenCache;
        }

        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static string CacheFilePath = Path.Combine(Shared.Settings.ExportFilePath, @"Office365TokenCache.cch");

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.Deserialize(File.Exists(CacheFilePath)
                    ? File.ReadAllBytes(CacheFilePath)
                    : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.TokenCache.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(CacheFilePath, args.TokenCache.Serialize());
                    // once the write operationtakes place restore the HasStateChanged bit to filse
                    args.TokenCache.HasStateChanged = false;
                }
            }
        }
    }
}
