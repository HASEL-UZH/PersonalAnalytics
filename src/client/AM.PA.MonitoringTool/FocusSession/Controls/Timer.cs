using Newtonsoft.Json;
using SlackAPI;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace FocusSession.Controls
{
    public class Timer
    {
        /* variables */
        private static DateTime startTime;
        private static DateTime endTime;
        private static System.Timers.Timer aTimer;
        private static System.Collections.Generic.List<Microsoft.Graph.Message> emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>(); // this list is simply to keep track of the already replied to emails during the session
        private static System.Collections.Generic.List<int> slackMessagesResponded = new System.Collections.Generic.List<int>();
        private static NotifyIcon notification; // workaround: this is to show a ballontip, if focusAssist is not set to 'alarms only', the user will see it. The icon itself will show that a focusSession is running
        private static int numberOfReceivedSlackMessages = 0;
        private static int numberOfReceivedEmailMessages = 0;
        private static SlackClient slackClient;
        private static int flaggerDisplayed = 0;
        private static string slackMemberId;
        private static string slackName;
        private static bool slackEnabledWorkspace = true;
        private static bool slackEnabledReply = true;
        private static bool emailEnabled = false;

        public static bool openSession { get; set; } = false;   // indicate if an openSession is running
        public static bool closedSession { get; set; } = false; // indicate if a closedSession is running
        public static bool WindowFlaggerMessageBoxActive { get; set; } = false;
        public static string ReplyMessage { get; set; }

        // get the setting from the database
        public static bool ReplyMessageEnabled { get; set; } = Data.Queries.GetReplyMessageEnabled();
        public static bool WindowFlaggingEnabled { get; set; } = Data.Queries.GetWindowFlaggingEnabled();
        public static bool CustomizedReplyMessageEnabled { get; set; } = Data.Queries.GetCustomizedReplyMessageEnabled();
        public static string CustomizedReplyMessage { get; set; } = Data.Queries.GetCustomizedReplyMessage();
        public static bool CustomizedFlaggingListEnabled { get; set; } = Data.Queries.GetCustomizedFlaggingListEnabled();
        public static string CustomizedFlaggingList { get; set; } = Data.Queries.GetCustomizedFlaggingList();

        // list of potentially distracting programs that we use for flagging check
        private static string[] windowFlaggerList = new string[] { "Skype", "WhatsApp", "Zoom", "Microsoft Outlook", "Google Hangouts", "Discord", "LINE", "Signal", "Trilian", "Viber", "Pidgin", "eM Client", "Thunderbird", "Whatsapp Web", "Facebook", "Winmail", "Telegram", "Yahoo Mail", "Camfrog", "Messenger", "TextNow", "Slack", "mIRC", "BlueMail", "Paltalk", "Mailbird", "Jisti", "Jabber", "OpenTalk", "ICQ", "Gmail", "Tango", "Lync", "Pegasus", "Mailspring", "Teamspeak", "QuizUp", "IGA", "Zello", "Jelly SMS", "Mammail", "Line", "MSN", "inSpeak", "Spark", "TorChat", "ChatBox", "AIM", "HexChat", "HydraIRC", "Mulberry", "Claws Mail", "Pandion", "ZChat", "Franz", "Microsoft Teams", "Zulip" };


        // declaring closedsessionComplete event for context menu
        public static event EventHandler ClosedSessionCompleted;

        // use null sender since static method
        protected static void OnProcessCompleted(EventArgs e)
        {
            ClosedSessionCompleted?.Invoke(null, e);
        }


        /* getter */

        // for icon hover information and email reply
        public static TimeSpan getSessionTime()  // get the current session Time
        {
            if (openSession)
            {
                return DateTime.Now - startTime;    // return for how long the open session has been running (= elapsed Time)
            }
            if (closedSession)
            {
                return endTime - startTime;         // return for how long the closed session will still be running (= remaining Time)
            }
            return TimeSpan.Zero;
        }

        /* main methods */

        // starts a session. Input: Enum if open or closed Session
        public static void StartSession(Enum.SessionEnum.Session session, int? sessionDuration = 0)
        {
            // check that there is not another session already running
            if (!openSession && !closedSession)
            {
                // set startTime
                startTime = DateTime.Now;

                if (session == Enum.SessionEnum.Session.openSession)
                {
                    // update indicator
                    openSession = true;

                    // log that the user started an openSession
                    Data.Queries.LogInfo("StartSession : The participant started an openFocusSession at " + DateTime.Now);

                    // set static automatic email reply message
                    // either the default one, or the one set by the user in the settings, retrieved from the database
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;          // retrieved from database
                    }
                    else
                    {
                        ReplyMessage = Settings.IsTextMessageByDefault; // default text
                    }
                }
                // start closedSession
                else if (session == Enum.SessionEnum.Session.closedSession)
                {
                    // add the timeperiod
                    if (sessionDuration != 0)
                    {                                         // Quickstart from submenu
                        endTime = DateTime.Now.AddMinutes((double)sessionDuration);     // set time with option from submenu  
                        // log that the user started a closedFocusSession
                        Data.Queries.LogInfo("StartSession : The participant started a closedFocusSession at " + DateTime.Now + " for " + (double)sessionDuration + " minutes.");
                    }
                    else
                    {
                        endTime = DateTime.Now.AddMinutes((double)Data.Queries.GetCustomizedSessionDuration());  // customTimer
                        // log that the user started a closedFocusSession
                        Data.Queries.LogInfo("StartSession : The participant started a closedFocusSession at " + DateTime.Now + " for " + Data.Queries.GetCustomizedSessionDuration() + " minutes.");
                    }

                    // update indicator
                    closedSession = true;

                    // set static automatic email reply message
                    // either the default one, or the one set by the user in the settings, retrieved from the database
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;      // retrieved from database
                    }
                    else
                    {
                        ReplyMessage = "\nThe recepient of this email is currently in a focused work session for another " + Data.Queries.GetCustomizedSessionDuration() + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";   // default message, with full time duration still remaining
                    }
                }

                // check if office365 tracker is enabled
                emailEnabled = Shared.Data.Database.GetInstance().GetSettingsBool("MsOfficeTrackerEnabled", false);

                // since there if no officially supported API by Microsoft to check the Focus assist status, we have this little workaround
                // if Focus assist is not active / not set to 'Priority only' nor 'Alarms only', the user will actually see the message. Otherwise, it will not show up. It is viewable in the Notifications tray, but will be disposed when a session is stopped.
                // The icon at the same time serves as indicator that there is an active session running
                notification = new NotifyIcon(); // make a new instance of the object, since when stopping the session, the instance will be disposed
                notification.Visible = true;
                notification.BalloonTipTitle = "FocusSession";
                notification.BalloonTipText = "Set FocusAssist to 'Alarms only'";
                notification.Icon = SystemIcons.Information;
                notification.Text = "FocusSession: Session active";
                notification.ShowBalloonTip(40000); // attempting maximum timeout value. This is enforced and handled by the operating system, typically 30 seconds is the max

                // set the timer, which also handles session functionality. We start a timer in the openSession to make use of the session functionality
                SetTimer();
            }
        }

        // Input if manually stopped or timed out
        public static void StopSession(Enum.SessionEnum.StopEvent stopEvent)
        {
            if (openSession || closedSession)
            {
                // get the current timestamp
                DateTime stopTime = DateTime.Now;

                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;

                // initialize endMessage to display to the participant
                StringBuilder endMessage = new StringBuilder("You did focus for " + elapsedTime.Hours + " hours and " + elapsedTime.Minutes + " Minutes. Good job :)");

                // specific to session type
                if (stopEvent == Enum.SessionEnum.StopEvent.manual || stopEvent == Enum.SessionEnum.StopEvent.paused)
                {
                    // log which session the user stopped
                    if (openSession)
                    {
                        if (stopEvent == Enum.SessionEnum.StopEvent.paused)
                        {
                            // store in log
                            Data.Queries.LogInfo("StopSession-Pause : The participant stopped an openFocusSession at " + DateTime.Now);
                            // store in focusTimer table database
                            Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "open-paused", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
                        }
                        else
                        {
                            // store in log
                            Data.Queries.LogInfo("StopSession : The participant stopped an openFocusSession at " + DateTime.Now);
                            // store in focusTimer table database
                            Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "open", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
                        }
                    }
                    else
                    {
                        if (stopEvent == Enum.SessionEnum.StopEvent.paused)
                        {
                            // store in log
                            Data.Queries.LogInfo("StopSession-Paused : The participant stopped a closedFocusSession at " + DateTime.Now);
                            // store in focusTimer table database
                            Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "closed-paused", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
                        }
                        else
                        {
                            // store in log
                            Data.Queries.LogInfo("StopSession : The participant stopped a closedFocusSession at " + DateTime.Now);
                            // store in focusTimer table database
                            Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "closed-manual", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
                        }
                    }

                    // update indicator. Manual means the user stopped an open Session or Cancelled a closed Session
                    openSession = false;
                    closedSession = false;
                }
                else
                {
                    // log that a closedFocusSession ran out
                    Data.Queries.LogInfo("StopSession : A closedFocusSession ran out at " + DateTime.Now);
                    // store in focusTimer table database
                    Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "closed-automatic", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);

                    // update indicator
                    closedSession = false;

                    // send event for context menu update
                    OnProcessCompleted(EventArgs.Empty);

                    // indicate that timer has run out in endMessage
                    endMessage.Insert(0, "FocusSession timer elapsed. ");
                }

                // also store in log
                Data.Queries.LogInfo("StopSession : The session had been running for " + elapsedTime);

                // stop if a timer is running
                if (aTimer != null && aTimer.Enabled)
                {
                    aTimer.Stop();
                    aTimer.Dispose();
                }

                // get the amount of time total focused for today
                TimeSpan totalDay = Data.Queries.GetFocusTimeFromDay(DateTime.Now);

                // get the amount of time total focused for this week
                TimeSpan totalWeek = Data.Queries.GetFocusTimeFromDay(StartOfWeek(DayOfWeek.Monday));

                // get the amount of time total focused for this month
                TimeSpan totalMonth = Data.Queries.GetFocusTimeFromDay(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));

                // information about active functionality
                if (slackEnabledWorkspace)
                {
                    endMessage.Append("\n\nSlack is enabled. ");

                    if (slackEnabledReply)
                    {
                        endMessage.Append("Slack does automatically reply in public channels added on user mention. ");
                    }
                    else
                    {
                        // no memberId(and memberName) has been provided in the slackConfig.json file
                        endMessage.Append("Slack does not automatically reply on user mention in added public channels. ");
                    }
                }
                else
                {
                    // no botToken for a workspace has been provided in the slackConfig.json file
                    endMessage.Append("\n\nSlack is not enabled. ");
                }

                if (emailEnabled)
                {
                    endMessage.Append("Email is enabled. ");

                    if (ReplyMessageEnabled)
                    {
                        endMessage.Append("Automatic Email reply is active.");
                    }
                    else
                    {
                        endMessage.Append("Automatic Email reply is not active.");
                    }
                }
                else
                {
                    endMessage.Append("Email is not enabled.");
                }
                endMessage.Append("\n\nMessages received during this session: \n");
                if (emailEnabled)
                {
                    endMessage.Append(numberOfReceivedEmailMessages + " Email. \n");
                }
                if (slackEnabledWorkspace)
                {
                    endMessage.Append(numberOfReceivedSlackMessages + " Slack.");
                }

                if (emailEnabled && ReplyMessageEnabled)
                {
                    endMessage.Append("\n\nEmails automatically replied to during this session: \n" + emailsReplied.Count);
                }
                if (slackEnabledReply)
                {
                    endMessage.Append("\n\nSlack mentions automatically replied to during this session: \n" + slackMessagesResponded.Count);
                }

                // time statistics
                endMessage.Append("\n\nTotal time focused this day: " + totalDay.Hours + " hours and " + totalDay.Minutes + " minutes.");
                endMessage.Append("\nTotal time focused this week: " + totalWeek.Hours + " hours and " + totalWeek.Minutes + " minutes.");
                endMessage.Append("\nTotal time focused this month: " + totalMonth.Hours + " hours and " + totalMonth.Minutes + " minutes.");

                // display a message to the user so the user gets feedback (important)
                MessageBox.Show("FocusSession stopped.", "FocusSession Summary");

                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                MessageBox.Show(endMessage.ToString(), "FocusSession Summary");

                // reset variables

                // int emails received is reset in each check, so no need to reset it here.

                numberOfReceivedSlackMessages = 0;

                // reset received messages. We want the last run number
                numberOfReceivedEmailMessages = 0;

                // empty replied Emails list
                emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>();

                // empty replies Slack list
                slackMessagesResponded = new System.Collections.Generic.List<int>();

                // reset flagger
                flaggerDisplayed = 0;

                // dispose notifications 
                notification.Dispose();
            }
        }

        // Input if manually stopped or timed out
        public static void Shutdown()
        {
            // get the current timestamp
            DateTime stopTime = DateTime.Now;

            // calculate the timespan
            TimeSpan elapsedTime = stopTime - startTime;

            // log shutdown
            Data.Queries.LogInfo("Participant Shutdown PersonalAnalytics with an active session running");

            // log which session the user stopped
            if (openSession)
            {
                // store in focusTimer table database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "open-shutdown", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
            }
            else
            {
                // store in focusTimer table database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime, "closed-shutdown", emailEnabled.ToString(), ReplyMessageEnabled.ToString(), numberOfReceivedEmailMessages, emailsReplied.Count, slackEnabledWorkspace.ToString(), slackEnabledReply.ToString(), numberOfReceivedSlackMessages, slackMessagesResponded.Count, flaggerDisplayed);
            }

            // stop if a timer is running
            if (aTimer != null && aTimer.Enabled)
            {
                aTimer.Stop();
                aTimer.Dispose();
            }

            // dispose notifications 
            notification.Dispose();
        }

        /* helper methods */

        private static void SetTimer()
        {
            // 10 sec interval, checking and replying to emails or ending session
            aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public static DateTime StartOfWeek(DayOfWeek startOfWeek)
        {
            DateTime dt = DateTime.Now;
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // check if this is a closed session where a timer actually runs out, and if we hit the endTime already
            if (closedSession && DateTime.Compare(DateTime.Now, endTime) > 0)
            {
                StopSession(Enum.SessionEnum.StopEvent.timed);
            }
            else
            {
                // slack

                // initialize slackClient & get config / settings
                InitializeSlackClient();
                // checks for total missed slack messages during session, in the corresponding workspace of the token, in channels where the bot has been addded to
                // Task.Result will block async code, and should be used carefully.
                if (slackEnabledWorkspace)
                {
                    numberOfReceivedSlackMessages = slackClient.CheckReceivedSlackMessagesInWorkspace().Result;
                }

                // email

                // set dynamic automatic email reply message
                if (emailEnabled)
                {
                    if (closedSession)
                    {
                        if (!CustomizedReplyMessageEnabled)
                        {
                            // update remaining time in message
                            ReplyMessage = "\nThe recepient of this email is currently in a focused work session for another " + getSessionTime().Hours + "hours and " + getSessionTime().Minutes + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
                        }
                    }
                    else
                    {
                        if (!CustomizedReplyMessageEnabled)
                        {
                            // update already running time in message
                            ReplyMessage = "\nThe recepient of this email is currently in a focused work session since " + getSessionTime().Hours + "hours and " + getSessionTime().Minutes + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
                        }
                    }
                    // this checks for missed emails and replies, adds replied emails to the list 'emailsReplied', which will be used at the end of the session to report on emails and then be emptied
                    await CheckMail();
                }
            }
        }

        private static async Task CheckMail()
        {

            // check mail and send an automatic reply if there was a new email.
            var unreadEmailsReceived = MsOfficeTracker.Helpers.Office365Api.GetInstance().GetUnreadEmailsReceived(DateTime.Now.Date);
            unreadEmailsReceived.Wait();

            // create message counter. Set to emailsReplied, since replied emails are not fetched anymore from the unreadEmails List
            int numberOfReceivedEmailMessagesCounter = emailsReplied.Count;

            foreach (Microsoft.Graph.Message email in unreadEmailsReceived.Result)
            {
                // check if this email had been received after the session started
                if (email.ReceivedDateTime.Value.LocalDateTime > startTime)
                {
                    // check if we have already replied to this email during this session
                    if (emailsReplied.Contains(email))
                    {
                        // do nothing, we already replied
                    }
                    // else reply to the email and add it to the emailsReplied List
                    else
                    {
                        if (ReplyMessageEnabled)
                        {
                            string address = email.From.EmailAddress.Address.ToLower();
                            // exclude emails that contain do not reply, or postmaster which sends a message if the mail could not be delivered
                            if (!address.Contains("do-not-reply") || !address.Contains("no-reply") || !address.Contains("noreply") || !address.Contains("postmaster@logmeininc.onmicrosoft.com"))
                            {
                                // send reply message
                                await MsOfficeTracker.Helpers.Office365Api.GetInstance().SendReplyEmail(email.Id, email.From.EmailAddress.Name, email.From.EmailAddress.Address, ReplyMessage);

                                //add email to list of already replied emails during this focus session
                                emailsReplied.Add(email);
                            }
                        }
                    }
                    // increase message counter
                    numberOfReceivedEmailMessagesCounter++;
                }
            }

            // update how many emails were received during session
            numberOfReceivedEmailMessages = numberOfReceivedEmailMessagesCounter;
        }

        private static void InitializeSlackClient()
        {
            if (System.IO.File.Exists(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json")))
            {
                // deserialized config.json to fetch tokens from class
                string allText = System.IO.File.ReadAllText(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json"));
                Configuration.SlackConfig slackConfig = JsonConvert.DeserializeObject<Configuration.SlackConfig>(allText);

                // initialize client
                slackClient = new SlackClient(slackConfig.botAuthToken);

                // extract memberId
                slackMemberId = slackConfig.memberId;

                // extract slackName
                slackName = slackConfig.memberName;

                // check for default values from config if slack is being used
                // check for workspace
                if (slackConfig.botAuthToken.Equals("bottoken-bottoken-bottoken-bottoken"))
                {
                    slackEnabledWorkspace = false;
                    // there is no connected workspace to respond to
                    slackEnabledReply = false;
                }
                else
                {
                    slackEnabledWorkspace = true;
                    // if workspace and public channels watched, check for memberId for automatic responce/ expectation management
                    if (!slackConfig.memberId.Equals("yourMemberId"))
                    {
                        slackEnabledReply = true;
                    }
                }

            }
        }

        // this method is called by the WindowsActivityTracker Demon, upon a foreground window/program switch, in case of an active FocusSession running
        // it checks if it is a potentially distracting program according to the list, currently printing to the Console
        public static void WindowFlagger(String currentWindowTitle)
        {
            if (WindowFlaggingEnabled)
            {
                var localWindowFlaggerList = windowFlaggerList;

                // we overwrite, this way we still keep the original list/values, but overwrite when user activates the setting during a session (no session restart needed)
                if (CustomizedFlaggingListEnabled)
                {
                    // replace whitespace after commata, and split it into an array for foreach loop
                    localWindowFlaggerList = CustomizedFlaggingList.Replace(", ", ",").Split(',');
                }

                foreach (String windowFlagger in localWindowFlaggerList)
                    if (currentWindowTitle.Contains(windowFlagger))
                    {
                        if (WindowFlaggerMessageBoxActive)
                        {
                            return;
                        }
                        else
                        {
                            // set active MessageBox. We do not want to stack boxes, user will also not know anymore which box would have belonged to which application in the end if user would just let them stack
                            WindowFlaggerMessageBoxActive = true;

                            // show message box to ask if this is task-related
                            var selectedOption = MessageBox.Show("You opened a potentially distracting program during an active FocusSession. Is " + currentWindowTitle + " related to the task you are currently focussing on?", "Potentially distracting Program detected: " + currentWindowTitle, MessageBoxButtons.YesNo);

                            // log the users answer
                            Data.Queries.LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " and was shown the WindowFlagger Messagebox");

                            // track how often the user has been shown the flagger message during a session
                            flaggerDisplayed++;

                            // check answer
                            // TODO store in database entry for study rather then just console-outprinting
                            if (selectedOption == DialogResult.Yes)

                            {
                                Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");

                                // log the users answer
                                Data.Queries.LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");

                                // user responded to messagebox
                                WindowFlaggerMessageBoxActive = false;

                            }
                            else if (selectedOption == DialogResult.No)

                            {
                                Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

                                // log the users answer
                                Data.Queries.LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

                                // user responded to messagebox
                                WindowFlaggerMessageBoxActive = false;
                            }
                        }
                    }

            }
        }

        private class SlackClient
        {
            readonly SlackTaskClient client;
            public SlackClient(string botToken)
            {
                client = new SlackTaskClient(botToken);
            }

            // this is a simple posting method for demonstration purposes
            internal async Task SendSlackMessage(string channelName)
            {

                // send simple message to general channel and wait for the call to complete
                var channel = channelName;
                var text = "The participant '" + slackName + "' mentioned in the previous message is currently in a focused work session and will receive your message after completing the current task.";
                // if default value / name not overwritten we display member id instead
                if (slackName.Equals("yourSlackName"))
                {
                    text = "The participant with the member ID '" + slackMemberId + "' mentioned in the previous message is currently in a focused work session and will receive your message after completing the current task.";
                }
                var response = await client.PostMessageAsync(channel, text);

                // process response from API call
                if (response.ok)
                {
                    Console.WriteLine("Message sent successfully");
                }
                else
                {
                    Console.WriteLine("Message sending failed. error: " + response.error);
                }

            }

            // Checks all channels from the workspace in which the focussession-bot had been added to (being watched), and returns a total sum or all missed messages
            internal async Task<int> CheckReceivedSlackMessagesInWorkspace()
            {
                // total number of missed messages to return
                int numberOfMissedMessages = 0;

                // get the list of all channels from that workspace. When testing, this only returns public channels.
                ChannelListResponse channelList = await client.GetChannelListAsync();

                // loop trough the channels in the workspace
                for (int channelCounter = 0; channelCounter < channelList.channels.Length; channelCounter++)
                {
                    // i could also return a list of messages missed per channel, so we can show the user detailed info on where he missed messages exactly (in which channel that is)
                    //var name = channelList.channels[channelCounter].name;

                    // check if the bot is a member of this channel
                    // remember this is using a bot-token. If it were with a user token, a more elegant way would be to check the channel for unread messages with 'channelList.channels[i].unread_count>0'
                    // and then on the channel itself, read the message history backwards, so loop thorugh it from latest to earliest, with earliest being the oldest unread message (channelMessageHistory.latest would fetch the reading cursor or the user, so the beginning of the yet unread messages.). 
                    if (channelList.channels[channelCounter].is_member)
                    {

                        // get message histroy
                        ChannelMessageHistory channelMessageHistory = await client.GetChannelHistoryAsync(channelList.channels[channelCounter]);

                        // loop thorugh the messages
                        for (int messageCounter = 0; messageCounter < channelMessageHistory.messages.Length; messageCounter++)
                        {

                            DateTime messageDate = channelMessageHistory.messages[messageCounter].ts; // Date of the message
                            // check if received after we started the focusSession
                            if (messageDate > startTime)
                            {
                                numberOfMissedMessages++;
                                // reply in public channel on user mention
                                if (slackEnabledReply)
                                {
                                    if (channelMessageHistory.messages[messageCounter].text.Contains(slackMemberId) && !slackMessagesResponded.Contains(channelMessageHistory.messages[messageCounter].id))
                                    {
                                        await SendSlackMessage(channelList.channels[channelCounter].name);
                                        slackMessagesResponded.Add(channelMessageHistory.messages[messageCounter].id);
                                    }
                                }
                            }
                            else
                            {
                                // jump out of loop, all other messages will also be older than the session start, we do not need to continue processing
                                messageCounter = channelMessageHistory.messages.Length;
                            }
                        }
                    }
                }

                // return total sum of missed messages
                return numberOfMissedMessages;
            }

        }
    }
}