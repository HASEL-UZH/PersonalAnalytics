// Created by André Meyer at MSR, updated at University of Zurich
// Created: 2015-12-09
// 
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Shared.Data;
using System.Runtime.InteropServices;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Logger = Shared.Logger;
using Message = Microsoft.Graph.Message;

namespace MsOfficeTracker.Helpers
{
    /// <summary>
    /// The Office365 uses the MSAL library for the authentication (previously ADAL) 
    /// and then the GraphServiceclient to have a nice interface for the API requests.
    /// 
    /// A good starting point for the authentication with MSAL can be found here: 
    /// https://docs.microsoft.com/en-us/azure/active-directory/develop/guidedsetups/active-directory-windesktop
    /// https://github.com/Azure-Samples/active-directory-dotnet-desktop-msgraph-v2/tree/master/active-directory-wpf-msgraph-v2
    /// 
    /// A good starting point for using the Graph library can be found here:
    /// https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/message
    /// https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/user_list_calendarview
    /// https://developer.microsoft.com/en-us/graph/graph-explorer
    /// 
    /// The app can be registered here: 
    /// https://apps.dev.microsoft.com/#/appList
    /// </summary>
    public class Office365Api
    {
        private static Office365Api _api;

        private string _clientId;
        private AuthenticationResult _authResult;
        private GraphServiceClient _client;
        private PublicClientApplication _app;
        private readonly string _authority = string.Format(CultureInfo.InvariantCulture, Settings.LoginApiEndpoint, "common"); // use microsoft.onmicrosoft.com for just this tenant, use "common" if used for everyone

        private const string DateTimeToFormatString = "yyyy-MM-ddTHH:mm:ssK";

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static Office365Api GetInstance()
        {
            return _api ?? (_api = new Office365Api());
        }

        #region Api Authentication, Clearing Cookies, etc.

        /// <summary>
        /// try logging in, or show sign-in page and ask for rights
        /// </summary>
        public async Task<bool> Authenticate()
        {
            var isAuthenticated = await TrySilentAuthentication();

            if (isAuthenticated)
            {
                var userName = (Shared.Settings.AnonymizeSensitiveData) ? Shared.Dict.Anonymized : _authResult.User.Name;
                Database.GetInstance().LogInfo("Successfully logged in with Office 365 (as " + userName + ")." );
                return true;
            }
            else
            {
                Database.GetInstance().LogError("Error logging in the user with Office 365.");
                return false;
            }
        }

        /// <summary>
        /// Checks if the connection to the API was successfully established previously.
        /// If not, it tries to establish a connection:
        /// 
        /// 1. check if an auth-token is available (usually then the connection works fine)
        /// 2. checks if internet is available (necessary)
        /// 3. checks if a silent authentication can be made. if it fails, tries to connect via regular SignIn.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectionToApiFailing()
        {
            return _authResult == null || ! IsInternetAvailable() || ! await TrySilentAuthentication();
        }

        /// <summary>
        /// This method is called from a method if the user is not properly signed in yet
        /// and to check if the user can be authenticated
        /// (also checks for an active internet connection)
        /// </summary>
        private async Task<bool> TrySilentAuthentication()
        {
            // check for internet connection
            if (! IsInternetAvailable()) return false;

            try
            {
                // receive API client ID
                if (_clientId == null)
                {
                    _clientId = GetOffice365ApiClientId();
                    if (_clientId == null) return false; 
                }

                // register app (if not yet done)
                if (_app == null)
                { 
                    _app = new PublicClientApplication(_clientId, _authority, FileCache.GetUserCache());
                }

                // Here, we try to get an access token to call the service without invoking any UI prompt.
                //_authResult = await _app.AcquireTokenAsync(_scopes, "", UIBehavior.ForceLogin, "");
                _authResult = await _app.AcquireTokenSilentAsync(Settings.Scopes, _app.Users.FirstOrDefault());

                // prepare outlook services client (if not yet ready)
                if (_client == null)
                {
                    _client = new GraphServiceClient(new DelegateAuthenticationProvider(requestMessage =>
                        {
                            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _authResult.AccessToken);
                            return Task.FromResult(0);
                        }));
                    //_client.BaseUrl = Settings.GraphApiEndpoint;
                }

                return true;
            }
            catch (MsalUiRequiredException)
            {
                // MSAL couldn't get a token silently, so show the user a message and let them click the Sign-In button.
                var res = await SignIn();
                return res;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return false;
            }
        }

        /// <summary>
        /// This method fetches the Office 365 API client secret from the PA-service server
        /// TODO: store it somewhere safely (e.g. see SecretStorage in FitbitTracker)
        /// </summary>
        /// <returns></returns>
        private string GetOffice365ApiClientId()
        {
            try
            {
                AccessDataService.AccessDataClient client = new AccessDataService.AccessDataClient();
                string clientId = client.GetOffice365ClientId();
                if (clientId != null)
                {
                     return clientId;
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return null;
        }

        /// <summary>
        /// force MSAL to prompt the user for credentials by specifying PromptBehavior.Always.
        /// MSAL will get a token and cache it
        /// </summary>
        private async Task<bool> SignIn()
        {
            try
            {
                _authResult = await _app.AcquireTokenAsync(Settings.Scopes);
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
        /// clear the MSAL token cache
        /// It's also necessary to clear the cookies from the browser' control so the next user has a chance to sign in.
        /// </summary>
        public void SignOut()
        {
            if (!_app.Users.Any()) return;
            try
            {
                _app.Remove(_app.Users.FirstOrDefault());
            }
            catch (MsalException ex)
            {
                Logger.WriteToLogFile(ex);
            }
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int description, int reservedValue);

        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }

        #endregion

        #region Meeting Queries

        /// <summary>
        /// Loads all meetings from the user's main calendar for the given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<Event>> LoadMeetings(DateTimeOffset date)
        {
            var meetings = new List<Event>();

            if (await ConnectionToApiFailing()) return meetings;

            try
            {
                var dtStart = date.Date.AddSeconds(1).ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();

                var options = new List<QueryOption>
                {
                    new QueryOption("startDateTime", dtStart.ToString(DateTimeToFormatString)),
                    new QueryOption("endDateTime", dtEnd.ToString(DateTimeToFormatString)),
                    //new QueryOption("select", "subject,body,bodyPreview,organizer,attendees,start,end,location"),
                };
                var result = await _client.Me.CalendarView.Request(options).GetAsync();
                //var calendar = await _client.Me.Calendar.CalendarView.Request(options).GetAsync();

                if (result?.Count > 0)
                {
                    var meetingsUnfiltered = new List<Event>();
                    meetingsUnfiltered.AddRange(result.CurrentPage);
                    while (result.NextPageRequest != null)
                    {
                        result = await result.NextPageRequest.GetAsync();
                        meetingsUnfiltered.AddRange(result.CurrentPage);
                    }

                    // remove unneeded meetings
                    meetings = meetingsUnfiltered.Where(
                                    m => (m.IsCancelled != null && m.IsCancelled.Value == false) // && // meeting is not cancelled
                                         //(m.IsOrganizer != null && !m.IsOrganizer.Value && (m.ResponseStatus.Response == ResponseType.Accepted || m.ResponseStatus.Response == ResponseType.TentativelyAccepted))) // user attends meeting
                                    ).ToList();
                }
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
        /// In case the email result contains an odata count, return the value
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static long GetResultCount(IMailFolderMessagesCollectionPage result)
        {
            return result.AdditionalData.ContainsKey("@odata.count") ? long.Parse(result.AdditionalData["@odata.count"].ToString()) : Settings.NoValueDefault;
            // here, we could also iterate through the result and manually count (if it didn't work), but why should we when the API does it?
        }

        /// <summary>
        /// Returns the number of unread emails currently in the inbox
        /// </summary>
        /// <returns>number of items, -1 in case of an error</returns>
        public async Task<long> GetNumberOfUnreadEmailsInInbox()
        {
            if (await ConnectionToApiFailing()) return Settings.NoValueDefault;

            try
            {
                var options = new List<QueryOption>
                {
                    new QueryOption("$filter", "isRead eq false"),
                    new QueryOption("$count", "true")
                };
                var result = await _client.Me.MailFolders.Inbox.Messages.Request(options).GetAsync();
                var unreadInboxSize = GetResultCount(result);
                return unreadInboxSize;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return Settings.NoValueDefault;
            }
        }

        /// <summary>
        /// Returns the total number of emails currently in the inbox (read and unread)
        /// </summary>
        /// <returns>number of items, -1 in case of an error</returns>
        public async Task<long> GetTotalNumberOfEmailsInInbox()
        {
            if (await ConnectionToApiFailing()) return Settings.NoValueDefault;

            try
            {
                var options = new List<QueryOption>
                {
                    //new QueryOption("$filter", "isRead eq false"), // we want the total list
                    new QueryOption("$count", "true")
                };
                var result = await _client.Me.MailFolders.Inbox.Messages.Request(options).GetAsync();
                var inboxSize = GetResultCount(result);
                return inboxSize;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return Settings.NoValueDefault;
            }
        }

        /// <summary>
        /// Returns a list of emails which were sent on a given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<long> GetNumberOfEmailsSent(DateTimeOffset date)
        {
            if (await ConnectionToApiFailing()) return Settings.NoValueDefault;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                var options = new List<QueryOption>
                {
                    new QueryOption("$filter", "sentDateTime ge " + dtStart.ToString(DateTimeToFormatString) + " and sentDateTime le " + dtEnd.ToString(DateTimeToFormatString)),
                    new QueryOption("$count", "true")
                };
                var result = await _client.Me.MailFolders.SentItems.Messages.Request(options).GetAsync();
                var numberEmailsSent = GetResultCount(result);
                return numberEmailsSent;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return Settings.NoValueDefault;
            }
        }

        /// <summary>
        /// Returns a list of emails which were received on a given date AND are unread 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<int> GetNumberOfUnreadEmailsReceived(DateTimeOffset date)
        {
            if (await ConnectionToApiFailing()) return Settings.NoValueDefault;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                var options = new List<QueryOption>
                {
                    new QueryOption("$filter", "receivedDateTime ge " + dtStart.ToString(DateTimeToFormatString) + " and receivedDateTime le " + dtEnd.ToString(DateTimeToFormatString) + " and isRead eq false"),
                };
                var result = await _client.Me.Messages.Request(options).GetAsync();

                if (result?.Count > 0)
                {
                    var receivedUnfilter = new List<Message>();
                    receivedUnfilter.AddRange(result.CurrentPage);
                    while (result.NextPageRequest != null)
                    {
                        result = await result.NextPageRequest.GetAsync();
                        receivedUnfilter.AddRange(result.CurrentPage);
                    }

                    // delete emails in IgnoreFolders and Drafts and IM-copies from S4B/Teams
                    var ignoreFolders = await GetFoldersToIgnoreIds();
                    var unreadEmailsReceived = receivedUnfilter.Count(m => m.IsDraft == false && !ignoreFolders.Contains(m.ParentFolderId) && m.Subject != "IM");
                    return unreadEmailsReceived;
                }
                return 0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return Settings.NoValueDefault;
        }

        /// <summary>
        /// Returns a list of emails which were received on a given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<int> GetTotalNumberOfEmailsReceived(DateTimeOffset date)
        {
            if (await ConnectionToApiFailing()) return Settings.NoValueDefault;

            try
            {
                var dtStart = date.Date.ToUniversalTime();
                var dtEnd = date.Date.AddDays(1).ToUniversalTime();

                var options = new List<QueryOption>
                {
                    new QueryOption("$filter", "receivedDateTime ge " + dtStart.ToString(DateTimeToFormatString) + " and receivedDateTime le " + dtEnd.ToString(DateTimeToFormatString)),
                };
                var result = await _client.Me.Messages.Request(options).GetAsync();

                if (result?.Count > 0)
                {
                    var receivedUnfilter = new List<Message>();
                    receivedUnfilter.AddRange(result.CurrentPage);
                    while (result.NextPageRequest != null)
                    {
                        result = await result.NextPageRequest.GetAsync();
                        receivedUnfilter.AddRange(result.CurrentPage);
                    }

                    // delete emails in IgnoreFolders and Drafts and IM-copies from S4B/Teams
                    var ignoreFolders = await GetFoldersToIgnoreIds();
                    var emailsReceived = receivedUnfilter.Count(m => m.IsDraft == false && !ignoreFolders.Contains(m.ParentFolderId) && m.Subject != "IM");

                    return emailsReceived;
                }
                return 0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return Settings.NoValueDefault;
        }

        private List<string> _emailFoldersToIgnore;

        /// <summary>
        /// Loads a list of folders to find the Ids of folders that should not be considered
        /// (Spam, Sent, Clutter, Deleted, etc.)
        /// Also caches it after first instantiation.
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetFoldersToIgnoreIds()
        {
            if (_emailFoldersToIgnore != null) return _emailFoldersToIgnore;
            _emailFoldersToIgnore = new List<string>();

            try
            {
                var folders = await _client.Me.MailFolders.Request().GetAsync();

                // get all mailbox-folders
                var folderList = new List<MailFolder>();
                folderList.AddRange(folders.CurrentPage);
                while (folders.NextPageRequest != null)
                {
                    folders = await folders.NextPageRequest.GetAsync();
                    folderList.AddRange(folders.CurrentPage);
                }

                // filter folders we want to ignore
                var foldersToIgnore = new List<string>{"deleted", "junk", "spam", "sent", "draft", "outbox", "clutter", "archive"};
                foreach (var item in folderList)
                {
                    foreach (var ignoreFolder in foldersToIgnore)
                    {
                        if (!item.DisplayName.ToLower().Contains(ignoreFolder)) continue;
                        _emailFoldersToIgnore.Add(item.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteToLogFile(ex);
            }

            return _emailFoldersToIgnore;
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

        //public async Task<string> GetPhotoForUser(string email)
        //{
        //    if (await ConnectionToApiFailing()) return string.Empty;

        //    try
        //    {
        //        //var userResult = await _client.Users.GetById(email).Photo.ExecuteAsync(); //.Select(u => u.Photo)
        //        //var userResult = await _client.Users.Where(u => u.Id.Equals(email)).ExecuteAsync(); //.Select(u => u.Photo)

        //        //Console.WriteLine(userResult + userResult2.CurrentPage.ToString() + userResult3.CurrentPage.ToString());

        //        //var result = userResult.ToString();

        //        return "";
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteToLogFile(e); // disable because many photos are not available
        //        return string.Empty;
        //    }
        //}

        #endregion
    }
}
