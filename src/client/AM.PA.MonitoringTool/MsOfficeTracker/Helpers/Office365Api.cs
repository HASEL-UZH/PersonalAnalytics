// Created by André Meyer at MSR
// Created: 2015-12-09
// 
// Licensed under the MIT License.
using System;
using System.Globalization;
using Microsoft.Office365.OutlookServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Shared.Data;
using MsOfficeTracker.Models;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Logger = Shared.Logger;

namespace MsOfficeTracker.Helpers
{
    public class Office365Api
    {
        private static Office365Api _api;
        //private Uri redirectUri = new Uri(Settings.RedirectUriString);
        //private string _authority = string.Format(CultureInfo.InvariantCulture, Settings.AadInstance, "common"); // use microsoft.onmicrosoft.com for just this tenant, use "common" if used for everyone
        //private AuthenticationContext _authContext;
        private AuthenticationResult _authResult;
        private OutlookServicesClient _client;
        private PublicClientApplication _app;

        private string[] _scopes = { "mail.read", "calendars.read" }; // "https://outlook.office.com/mail.read", "https://outlook.office.com/calendars.read" }; // "https://outlook.office.com/user.readbasic.all" };
        private const string _graphAPIEndpoint = "https://outlook.office.com/api/v2.0"; //"https://graph.microsoft.com/v1.0/me"; 
        //private string _loggedInUserEmail;
        //private string _loggedInUserName;

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static Office365Api GetInstance()
        {
            return _api ?? (_api = new Office365Api());
        }

        #region Api Authentication, Clearing Cookies, etc.

        private async void Test()
        {
            try
            {
                _app = new PublicClientApplication(Settings.ClientId, "https://login.microsoftonline.com/common",
                    FileCache.GetUserCache());

                //_authResult = await _app.AcquireTokenAsync(_scopes, "", UIBehavior.ForceLogin, "");
                await TrySilentAuthentication();

                var token = _authResult.AccessToken;
                _client = new OutlookServicesClient(new Uri("https://outlook.office.com/api/v2.0"),
                    () =>
                    {
                        return Task.Run(() => token);
                    });


                var num = await GetTotalNumberOfEmailsReceived(DateTime.Now.Date);
                Console.WriteLine(num);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Office365Api()
        {
            Test();
            //_authResult = await _app.AcquireTokenAsync(_scopes, "", UIBehavior.ForceLogin, "");

            //TrySilentAuthentication();

            // use file cache to persist token
            //_authContext = new AuthenticationContext(_authority, new FileCache());

            /*            
                        if (_authContext.TokenCache.ReadItems().Count() > 0)
                        {
                            // re-bind the AuthenticationContext to the authority that sourced the token in the cache 
                            // this is needed for the cache to work when asking a token from that authority 
                            // (the common endpoint never triggers cache hits) 
                            var cachedAuthority = _authContext.TokenCache.ReadItems().First().Authority;
                            _authContext = new AuthenticationContext(cachedAuthority, new FileCache());
                        }
                        else
                        {
                            // no previous tokens -> do nothing for now
                        }
            */

            // initialize outlook services client
            //if (_authResult != null)
            //{
            //    _client = new OutlookServicesClient(new Uri(_graphAPIEndpoint), async () =>
            //    {
            //        // Since we have it locally from the Session, just return it here.
            //        return _authResult.AccessToken; // was: .Token;
            //    });
            //}
            //else
            //{
            //    // TODO:
            //    Console.WriteLine("arrgh");
            //}
        }

        /// <summary>
        /// This method is called from a method if the user is not properly signed in yet
        /// and to check if the user can be authenticated
        /// (also checks for an active internet connection)
        /// </summary>
        private async Task<bool> TrySilentAuthentication()
        {
            try
            {
                // Here, we try to get an access token to call the service without invoking any UI prompt.  PromptBehavior.Never forces
                // ADAL to throw an exception if it cannot get a token silently.
                _authResult = await _app.AcquireTokenSilentAsync(_scopes, _app.Users.FirstOrDefault()); // _authContext.AcquireTokenAsync(_scopes, null, Settings.ClientId, redirectUri, new PlatformParameters(PromptBehavior.Never, null));
                return true;
            }
            catch (MsalUiRequiredException ex)
            {
                // ADAL couldn't get a token silently, so show the user a message
                // and let them click the Sign-In button.
                var res = await SignIn();
                return res;

                //if (ex.ErrorCode == "user_null") //"user_interaction_required")
                //{
                //    var res = await SignIn();
                //    return res;
                //}
                //else
                //{
                //    // In any other case, an unexpected error occurred.
                //    var message = ex.Message;
                //    if (ex.InnerException != null)
                //    {
                //        message += "Inner Exception : " + ex.InnerException.Message;
                //    }
                //    Logger.WriteToLogFile(ex);
                //    return false;
                //}
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return false;
            }
        }

        /// <summary>
        /// force ADAL to prompt the user for credentials by specifying PromptBehavior.Always.
        /// ADAL will get a token and cache it
        /// </summary>
        private async Task<bool> SignIn()
        {
            try
            {
                _authResult = await _app.AcquireTokenAsync(_scopes); //_authContext.AcquireTokenAsync(_scopes, null, Settings.ClientId, redirectUri, new PlatformParameters(PromptBehavior.Always, null));
                //_loggedInUserEmail = _authResult.UserInfo.DisplayableId; // hint: UserInfo empty after authentication
                //_loggedInUserName = _authResult.UserInfo.Name; // hint: UserInfo empty after authentication
                return true;
            }
            catch (MsalException ex)
            {
                // If MSAL cannot get a token, it will throw an exception.
                // If the user canceled the login, it will result in the
                // error code 'authentication_canceled'.
                if (ex.ErrorCode == "authentication_canceled")
                {
                    //MessageBox.Show("We could not connect to your Office 365 account as you canceled the authentication process. Please try again later.");
                    Database.GetInstance().LogWarning("Office 365 sign in was canceled by the user");
                    return false;
                }
                else
                {
                    // An unexpected error occurred.
                    var message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Inner Exception : " + ex.InnerException.Message;
                    }
                    Logger.WriteToLogFile(ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLogFile(ex);
                return false;
            }
        }

        /// <summary>
        /// clear the ADAL token cache
        /// It's also necessary to clear the cookies from the browser' control so the next user has a chance to sign in.
        /// </summary>
        public void SignOut()
        {
            if (_app.Users.Any())
            {
                try
                {
                    _app.Remove(_app.Users.FirstOrDefault());
                }
                catch (MsalException ex)
                {
                    // TODO: handle exceptions
                    Console.WriteLine(ex);
                }
            }

            //if (_authContext != null && _authContext.TokenCache != null) _authContext.TokenCache.Clear();
            //ClearCookies();
            //Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "Successfully signed-out user from Office 365."));
        }

        /// <summary>
        /// This function clears cookies from the browser control used by ADAL.
        /// </summary>
        //private void ClearCookies()
        //{
        //    const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
        //    NativeMethods.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        //}

        /// <summary>
        /// try logging in, or show sign-in page and ask for rights
        /// </summary>
        public async Task<bool> Authenticate()
        {
            var res = await TrySilentAuthentication();
            if (res)
            {
                Database.GetInstance().LogInfo("Successfully logged in with Office 365.");
                return true;
            }
            else
            {
                Database.GetInstance().LogError("Error logging in the user with Office 365.");
                return false;
            }
        }

        //public bool IsAuthenticatedUser(string name, string email)
        //{
        //    return (name == _loggedInUserName || email == _loggedInUserEmail);
        //}

        /// <summary>
        /// Returns true if the user is already authenticated with Office 365
        /// </summary>
        /// <returns></returns>
        public bool IsAuthenticated()
        {
            //if (!await TrySilentAuthentication() || _authResult == null) return false;
            if (_authResult == null) return false;
            return true;
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);

        private static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }

        #endregion

        #region Meeting Queries

        public async Task<List<DisplayEvent>> LoadMeetings(DateTimeOffset date)
        {
            var meetings = new List<DisplayEvent>();

            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return meetings;

            try
            {
                var groups = await _client.Me.Calendar.GetCalendarView(date.Date.ToUniversalTime(), date.Date.ToUniversalTime().AddDays(1).AddTicks(-1))
                                    .Where(e => e.IsCancelled == false)
                                    .OrderBy(e => e.Start.DateTime)
                                    .Take(20)
                                    .Select(e => new DisplayEvent(e.Organizer, e.IsOrganizer, e.Subject, e.ResponseStatus, e.Start.DateTime, e.End.DateTime, e.Attendees))
                                    .ExecuteAsync();

                do
                {
                    foreach (var m in groups.CurrentPage.ToList())
                    {
                        // only add if the user attends the meeting
                        if (!m.IsOrganizer && m.ResponseStatus != ResponseType.Accepted) continue;
                        meetings.Add(m);
                    }

                    groups = await groups.GetNextPageAsync();
                }
                while (groups != null && groups.MorePagesAvailable);                
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return meetings;
        }

        //public async Task<List<ContactItem>> LoadPeopleFromMeetings(DateTimeOffset date)
        //{
        //    var attendees = new List<ContactItem>();

        //    try
        //    {
        //        var meetings = await LoadMeetings(date);

        //        foreach (var m in meetings)
        //        {
        //            if (!m.IsOrganizer)
        //            {
        //                var email = (m.Organizer != null) ? m.Organizer.Address : string.Empty;
        //                var name = (m.Organizer != null) ? m.Organizer.Name : string.Empty;
        //                var c = new ContactItem(name, email, 1);

        //                attendees.Add(c);
        //            }

        //            if (m.Attendees.Count >= 0)
        //            {
        //                foreach (var a in m.Attendees)
        //                {
        //                    var c = new ContactItem("", a, 1);

        //                    attendees.Add(c);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteToLogFile(e);
        //    }

        //    return attendees;
        //}

        //internal async void LoadEvents()
        //{
        //    if (!IsInternetAvailable() || !await TrySilentAuthentication()) return;

        //    try
        //    {
        //        var token = authResult.Token;


        //        ////////////// ////////////// ////////////// 
        //        ////////////// Access the API
        //        ////////////// ////////////// ////////////// 

        //        var client = new OutlookServicesClient(new Uri(apiUrl), async () =>
        //        {
        //            // Since we have it locally from the Session, just return it here.
        //            return token;
        //        });

        // Once the token has been returned by ADAL, 
        // add it to the http authorization header, 
        // before making the call to access the To Do list service.
        //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

        ///////////// QUERY 1
        //var res = await httpClient.GetAsync("https://outlook.office.com/api/v2.0/me/calendarview?startdatetime=2015-12-08T08:00:00.000Z&enddatetime=2015-12-09T08:00:00.000Z");
        //var res = await httpClient.GetAsync("https://outlook.office.com/api/v2.0/Me/Events?$top=10&$select=Subject,Start,End");
        //var text = await res.Content.ReadAsStringAsync();
        //Console.WriteLine(text);


        ///////////// QUERY 2
        //var eventResults = await client.Me.Events
        //                    .OrderByDescending(e => e.Start.DateTime)
        //                    .Take(10)
        //                    .Select(e => new DisplayEvent(e.Subject, e.Start.DateTime, e.End.DateTime, e.Attendees.Count))
        //                    .ExecuteAsync();

        // Obtain calendar event data
        //var eventsResults = await (from i in client.Me.Events where i.End >= DateTimeOffset.UtcNow select i).Take(10).ExecuteAsync();

        //Console.WriteLine(eventResults);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
        //}

        #endregion

        #region Email Queries

        /// <summary>
        /// Returns the number of unread emails currently in the inbox
        /// </summary>
        /// <returns>number of items, -1 in case of an error</returns>
        public async Task<long> GetNumberOfUnreadEmailsInInbox()
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return -1;

            try
            {
                var groups = await _client.Me.MailFolders.GetById("Inbox").Messages
                    .Where(m => m.IsRead == false) // only unread emails
                    .Take(20)
                    .Select(m => new { m.From }) // only get single info (can get more if needed)
                    .ExecuteAsync();

                var inboxSize = 0;

                do
                {
                    var mailResults = groups.CurrentPage.ToList();
                    inboxSize += mailResults.Count;
                    groups = await groups.GetNextPageAsync(); // next page
                }
                while (groups != null); // && groups.MorePagesAvailable);

                return inboxSize;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return -1;
            }
        }

        /// <summary>
        /// Returns the total number of emails currently in the inbox (read and unread)
        /// </summary>
        /// <returns>number of items, -1 in case of an error</returns>
        public async Task<long> GetTotalNumberOfEmailsInInbox()
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return -1;

            try
            {
                var groups = await _client.Me.MailFolders.GetById("Inbox").Messages
                    .Take(20)
                    .Select(m => new { m.From }) // only get single info (can get more if needed)
                    .ExecuteAsync();

                var inboxSize = 0;

                do
                {
                    var mailResults = groups.CurrentPage.ToList();
                    inboxSize += mailResults.Count;
                    groups = await groups.GetNextPageAsync(); // next page
                }
                while (groups != null); // && groups.MorePagesAvailable);

                return inboxSize;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return -1;
            }
        }

        /// <summary>
        /// Returns a list of emails which were sent on a given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<int> GetNumberOfEmailsSent(DateTimeOffset date)
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return -1;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                var groups = await _client.Me.MailFolders.GetById("SentItems").Messages
                    .Where(m => m.SentDateTime.Value >= dtStart && m.SentDateTime.Value <= dtEnd)
                    .Take(20)
                    .Select(m => new { m.From }) //new DisplayEmail(m))
                    .ExecuteAsync();

                var numberEmailsSent = 0;
                do
                {
                    var mailResults = groups.CurrentPage.ToList();
                    numberEmailsSent += mailResults.Count;
                    groups = await groups.GetNextPageAsync(); // next page
                }
                while (groups != null); // && groups.MorePagesAvailable);

                return numberEmailsSent;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return -1;
            }
        }

        /// <summary>
        /// Returns a list of emails which were received on a given date
        /// AND are unread (and not in junk or delete folder)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<int> GetNumberOfUnreadEmailsReceived(DateTimeOffset date)
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return -1;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                // TODO: bug: this query also includes sent items (and not just received ones)
                var groups = await _client.Me.Messages
                    .OrderByDescending(m => m.ReceivedDateTime)
                    .Where(m => m.ReceivedDateTime.Value >= dtStart && m.ReceivedDateTime.Value <= dtEnd
                             && m.IsDraft == false && m.IsRead == false)  // only unread emails                                                                                                                                                //todo: filter if not in Junk Email and Deleted Folder (maybe with ParentFolderId)
                    .Take(20)
                    .Select(m => new { m.ParentFolderId }) // new DisplayEmail(m)) // m.From
                    .ExecuteAsync();

                var deleteFolders = await GetDeleteAndJunkFolderIds();

                var numberOfEmailsReceived = 0;
                do
                {
                    var mailResults = groups.CurrentPage.ToList();
                    numberOfEmailsReceived += mailResults.Count(m => !deleteFolders.Contains(m.ParentFolderId));

                    //if (deleteFolders.Item1 == false)
                    //{
                    //    numberOfEmailsReceived += mailResults.Count;
                    //}
                    //else
                    //{
                    //    numberOfEmailsReceived += mailResults.Where(m => m.ParentFolderId != deleteFolders.Item2 && m.ParentFolderId != deleteFolders.Item3 // not in deleted folder
                    //                                                && m.ParentFolderId != deleteFolders.Item4 && m.ParentFolderId != deleteFolders.Item5).ToList().Count; // not in junk folder
                    //}

                    groups = await groups.GetNextPageAsync();
                }
                while (groups != null); //&& groups.MorePagesAvailable);

                return numberOfEmailsReceived;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return -1;
            }
        }

        /// <summary>
        /// Returns a list of emails which were received on a given date
        /// (and not in junk or delete folder)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<int> GetTotalNumberOfEmailsReceived(DateTimeOffset date)
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return -1;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                // TODO: bug: this query also includes sent items (and not just received ones)
                var groups = await _client.Me.Messages
                    .Where(m => m.ReceivedDateTime.Value >= dtStart && m.ReceivedDateTime.Value <= dtEnd && m.IsDraft == false)
                    .OrderByDescending(m => m.ReceivedDateTime)
                    .Take(20)
                    .Select(m => new { m.ParentFolderId, m.ReceivedDateTime, m.Subject, m.From }) // new DisplayEmail(m)) // m.From
                    .ExecuteAsync();

                // filter if not in Junk Email and Deleted Folder with ParentFolderId
                var deleteFolders = await GetDeleteAndJunkFolderIds();
                var numberOfEmailsReceived = 0;
                do
                {
                    var mailResults = groups.CurrentPage.ToList();
                    numberOfEmailsReceived += mailResults.Count(m => !deleteFolders.Contains(m.ParentFolderId));

                    //if (deleteFolders.Count == 0)
                    //{
                    //    numberOfEmailsReceived += mailResults.Count;
                    //}
                    //else
                    //{
                    //    //numberOfEmailsReceived += mailResults.Where(m => m.ParentFolderId != deleteFolders.Item2 && m.ParentFolderId != deleteFolders.Item3 // not in deleted folder
                    //    //                                            && m.ParentFolderId != deleteFolders.Item4 && m.ParentFolderId != deleteFolders.Item5).ToList().Count; // not in junk folder
                    //}

                    groups = await groups.GetNextPageAsync();
                }
                while (groups != null); //&& groups.MorePagesAvailable);

                return numberOfEmailsReceived;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return -1;
            }
        }

        /// <summary>
        /// Loads a list of folders to find the Ids of the junk and deleted items folders
        /// to filter them
        /// 
        /// TODO: cache list
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetDeleteAndJunkFolderIds()
        {
            var foldersToDelete = new List<string>();

            try
            {
                var folders = await _client.Me.MailFolders.Take(10).Select(f => new { f.Id, f.DisplayName }).ExecuteAsync();
                do
                {
                    var res1 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("deleted")).FirstOrDefault();
                    if (res1 != null && !string.IsNullOrEmpty(res1.Id)) foldersToDelete.Add(res1.Id);

                    var res2 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("gelöscht")).FirstOrDefault();
                    if (res2 != null && !string.IsNullOrEmpty(res2.Id)) foldersToDelete.Add(res2.Id);

                    var res3 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("junk")).FirstOrDefault();
                    if (res3 != null && !string.IsNullOrEmpty(res3.Id)) foldersToDelete.Add(res3.Id);

                    var res4 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("spam")).FirstOrDefault();
                    if (res4 != null && !string.IsNullOrEmpty(res4.Id)) foldersToDelete.Add(res4.Id);

                    var res5 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("sent")).FirstOrDefault();
                    if (res5 != null && !string.IsNullOrEmpty(res5.Id)) foldersToDelete.Add(res5.Id);

                    var res6 = folders.CurrentPage.Where(f => f.DisplayName.ToLower().Contains("gesendet")).FirstOrDefault();
                    if (res6 != null && !string.IsNullOrEmpty(res6.Id)) foldersToDelete.Add(res6.Id);

                    //if (!string.IsNullOrEmpty(deletedFolderIdEn) && !string.IsNullOrEmpty(deletedFolderIdDe) &&
                    //    !string.IsNullOrEmpty(junkFolderId1) && !string.IsNullOrEmpty(junkFolderId2)) break;

                    folders = await folders.GetNextPageAsync();
                }
                while (folders != null);
            }
            catch
            {
                Console.WriteLine("TODO");
            }

            return foldersToDelete;
        }

        //public async Task<List<ContactItem>> LoadPeopleFromEmails(DateTimeOffset date)
        //{
        //    var attendees = new List<ContactItem>();

        //    if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return attendees;

        //    try
        //    {
        //        var emailsSent = await GetEmailsSent(date);

        //        foreach (var e in emailsSent)
        //        {
        //            foreach (var r in e.Recepients)
        //            {
        //                var c = new ContactItem(r.Name, r.Address, 1);
        //                attendees.Add(c);
        //            }
        //        }

        //        var emailsReceived = await GetEmailsReceived(date);
        //        foreach (var e in emailsReceived)
        //        {
        //            var email = (e.Sender != null) ? e.Sender.Address : string.Empty;
        //            var name = (e.Sender != null) ? e.Sender.Name : string.Empty;

        //            var c = new ContactItem(name, email, 1);

        //            attendees.Add(c);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteToLogFile(e);
        //    }

        //    return attendees;
        //}

        //public async Task<List<MyMessage>> GetMessages(int pageIndex, int pageSize)
        //{

        //    var client = await EnsureClientCreated();

        //    var messageResults = await (from message in client.Me.Messages
        //                                orderby message.DateTimeSent descending
        //                                select message)
        //                              .Skip(pageIndex * pageSize)
        //                              .Take(pageSize)
        //                              .ExecuteAsync();

        //    MorePagesAvailable = messageResults.MorePagesAvailable;

        //    var messageList = new List<MyMessage>();

        //    foreach (IMessage message in messageResults.CurrentPage)
        //    {
        //        var myMessage = new MyMessage
        //        {
        //            Id = message.Id,
        //            Subject = message.Subject,
        //            DateTimeReceived = message.DateTimeReceived,
        //            FromName = message.From.EmailAddress.Name,
        //            FromEmailAddress = message.From.EmailAddress.Address,
        //            ToName = message.ToRecipients[0].EmailAddress.Name,
        //            ToEmailAddress = message.ToRecipients[0].EmailAddress.Address,
        //            HasAttachments = message.HasAttachments
        //        };

        //        messageList.Add(myMessage);
        //    }
        //    return messageList;
        //}

        #endregion

        #region Contact Queries

        public async Task<string> GetPhotoForUser(string email)
        {
            if (!IsInternetAvailable() || !await TrySilentAuthentication() || _authResult == null) return string.Empty;

            try
            {
                //var userResult = await _client.Users.GetById(email).Photo.ExecuteAsync(); //.Select(u => u.Photo)
                //var userResult = await _client.Users.Where(u => u.Id.Equals(email)).ExecuteAsync(); //.Select(u => u.Photo)

                //Console.WriteLine(userResult + userResult2.CurrentPage.ToString() + userResult3.CurrentPage.ToString());

                // todo: handle current page
                //var result = userResult.ToString();

                return "";
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e); // todo; disable because many photos are not available
                return string.Empty;
            }
        }

        #endregion
    }
}
