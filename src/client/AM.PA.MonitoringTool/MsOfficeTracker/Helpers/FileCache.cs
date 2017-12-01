// Created by André Meyer at MSR
// Created: 2015-12-08
// 
// Licensed under the MIT License.
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace MsOfficeTracker.Helpers
{

    /// <summary>
    /// This is a simple persistent cache implementation for a desktop application.
    /// It uses DPAPI for storing tokens in a local file.
    /// 
    /// from: https://github.com/Azure-Samples/active-directory-dotnet-native-aspnet5/blob/master/TodoListClient/FileCache.cs
    /// </summary>
    internal class FileCache : TokenCache
    {
        public string CacheFilePath;
        private static readonly object FileLock = new object();

        // Initializes the cache against a local file.
        // If the file is already rpesent, it loads its content in the ADAL cache
        public FileCache()
        {
            CacheFilePath = Path.Combine(Shared.Settings.ExportFilePath, @"Office365TokenCache.cch");
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            lock (FileLock)
            {
                this.Deserialize(File.Exists(CacheFilePath) ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser) : null);
            }
        }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            File.Delete(CacheFilePath);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                this.Deserialize(File.Exists(CacheFilePath) ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser) : null);
            }
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(CacheFilePath, ProtectedData.Protect(this.Serialize(), null, DataProtectionScope.CurrentUser));
                    // once the write operation took place, restore the HasStateChanged bit to false
                    this.HasStateChanged = false;
                }
            }
        }
    }

}
