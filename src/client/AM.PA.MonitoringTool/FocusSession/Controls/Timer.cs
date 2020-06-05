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
        private static NotifyIcon notification; // workaround: this is to show a ballontip, if focusAssist is not set to 'alarms only', the user will see it. The icon itself will show that a focusSession is running
        private static int numberOfReceivedSlackMessages = 0;
        private static bool slackClientInitialized = false;
        private static SlackClient slackClient;
        private static int slackTestMessageLimit = 3;

        public static bool openSession { get; set; } = false;   // indicate if an openSession is running
        public static bool closedSession { get; set; } = false; // indicate if a closedSession is running
        public static bool WindowFlaggerMessageBoxActive { get; set; } = false;
        public static string ReplyMessage { get; set; }

        // get the setting from the database, it will default to true
        public static bool ReplyMessageEnabled { get; set; } = Shared.Data.Database.GetInstance().GetSettingsBool(Settings.REPLYMESSAGE_ENEABLED_SETTING, true);
        public static bool WindowFlaggingEnabled { get; set; } = Shared.Data.Database.GetInstance().GetSettingsBool(Settings.WINDOWFLAGGING_ENEABLED_SETTING, true);
        public static bool CustomizedReplyMessageEnabled { get; set; } = Shared.Data.Database.GetInstance().GetSettingsBool(Settings.CUSTOMIZEDREPLYMESSAGE_ENEABLED_SETTING, true);
        public static string CustomizedReplyMessage { get; set; } = Shared.Data.Database.GetInstance().GetSettingsString(Settings.CUSTOMIZEDREPLYMESSAGE_TEXT_SETTING, Settings.IsTextMessageByDefault);
        public static bool CustomizedFlaggingListEnabled { get; set; } = Shared.Data.Database.GetInstance().GetSettingsBool(Settings.CUSTOMIZEDFLAGGINGLIST_ENEABLED_SETTING, true);
        public static string CustomizedFlaggingList { get; set; } = Shared.Data.Database.GetInstance().GetSettingsString(Settings.CUSTOMIZEDFLAGGINGLIST_TEXT_SETTING, Settings.IsTextListByDefault);

        // list of potentially distracting programs that we use for flagging check
        private static string[] windowFlaggerList = new string[] { "Skype", "WhatsApp", "Zoom", "Microsoft Outlook", "Google Hangouts", "Discord", "LINE", "Signal", "Trilian", "Viber", "Pidgin", "eM Client", "Thunderbird", "Whatsapp Web", "Facebook", "Winmail", "Telegram", "Yahoo Mail", "Camfrog", "Messenger", "TextNow", "Slack", "mIRC", "BlueMail", "Paltalk", "Mailbird", "Jisti", "Jabber", "OpenTalk", "ICQ", "Gmail", "Tango", "Lync", "Pegasus", "Mailspring", "Teamspeak", "QuizUp", "IGA", "Zello", "Jelly SMS", "Mammail", "Line", "MSN", "inSpeak", "Spark", "TorChat", "ChatBox", "AIM", "HexChat", "HydraIRC", "Mulberry", "Claws Mail", "Pandion", "ZChat", "Franz", "Microsoft Teams", "Zulip" };

        /* getter */

        // for icon hover information and email reply
        public static int getSessionTime()  // get the current session Time
        {
            if (openSession)
            {
                return (DateTime.Now - startTime).Minutes;    // return for how long the open session has been running (= elapsed Time)
            }
            if (closedSession)
            {
                return Settings.ClosedSessionDuration - (DateTime.Now - startTime).Minutes;         // return for how long the closed session will still be running (= remaining Time)
            }
            return 0;
        }

        /* main methods */

        // starts a session. Input: Enum if open or closed Session
        public static void StartSession(Enum.SessionEnum.Session session)
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
                    Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant started an openFocusSession at " + DateTime.Now);

                    // set static automatic email reply message
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;
                    }
                    else
                    {
                        ReplyMessage = Settings.IsTextMessageByDefault;
                    }
                }
                // start closedSession
                else if (session == Enum.SessionEnum.Session.closedSession)
                {
                    // add the timeperiod, default is Pomodoro Timer 25 min, unless changed through the Settings
                    endTime = DateTime.Now.AddMinutes(Settings.ClosedSessionDuration);

                    // update indicator
                    closedSession = true;

                    // log that the user started a closedFocusSession
                    Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant started a closedFocusSession at " + DateTime.Now + " for " + Settings.ClosedSessionDuration + " minutes.");

                    // set dynamic automatic email reply message
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;
                    }
                    else
                    {
                        ReplyMessage = "\nThe recepient of this email is currently in a focused work session for another " + Settings.ClosedSessionDuration + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
                    }
                }

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
            else if (openSession)
            {
                // log that the user tried to start a session
                Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant tried to start a session with an active openSession already running)");
            }
            else
            {
                // log that the user tried to start a session
                Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant tried to start a session with an active closedSession already running)");
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
                if (stopEvent == Enum.SessionEnum.StopEvent.manual)
                {
                    // log which session the user stopped
                    if (openSession)
                    {
                        Shared.Data.Database.GetInstance().LogInfo("StopSession : The participant stopped an openFocusSession at " + DateTime.Now);
                    }
                    else
                    {
                        Shared.Data.Database.GetInstance().LogInfo("StopSession : The participant stopped a closedFocusSession at " + DateTime.Now);
                    }

                    // update indicator. Manual means the user stopped an open Session or Cancelled a closed Session
                    openSession = false;
                    closedSession = false;
                }
                else
                {
                    // log that a closedFocusSession ran out
                    Shared.Data.Database.GetInstance().LogInfo("StopSession : A closedFocusSession ran out at " + DateTime.Now);

                    // update indicator
                    closedSession = false;

                    // indicate that timer has run out in endMessage
                    endMessage.Insert(0, "FocusSession timer elapsed. ");
                }

                // store in database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime);

                // also store in log
                Shared.Data.Database.GetInstance().LogInfo("StopSession : The session had been running for " + elapsedTime);

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

                // messages received during session
                // TODO sort after number of messages received
                endMessage.Append("\n\nMessages received during this session: \n" + emailsReplied.Count + " Email \n" + numberOfReceivedSlackMessages + " Slack");

                // time statistics
                endMessage.Append("\n\nTotal time focused this day: " + totalDay.Hours + " hours and " + totalDay.Minutes + " minutes.");
                endMessage.Append("\nTotal time focused this week: " + totalWeek.Hours + " hours and " + totalWeek.Minutes + " minutes.");
                endMessage.Append("\nTotal time focused this month: " + totalMonth.Hours + " hours and " + totalMonth.Minutes + " minutes.");

                // display a message to the user so the user gets feedback (important)
                MessageBox.Show("FocusSession stopped.");

                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                MessageBox.Show(endMessage.ToString());

                // empty replied Emails list
                emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>();

                // reset SlackMessages
                numberOfReceivedSlackMessages = 0;

                notification.Dispose();
            }
            else
            {
                // log that the user tried to stop a session but there was no session currently running
                Shared.Data.Database.GetInstance().LogInfo("StartSession : The participant tried to stop a session with no active session running)");
            }
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

                // initialize slackClient if not already
                if (!slackClientInitialized)
                {
                    InitializeSlackClient();
                }
                else
                {
                    if (numberOfReceivedSlackMessages < slackTestMessageLimit && closedSession)
                    {
                        // this method is currently for demonstration purposes, the bot will simply post/spam a message in the general channel
                        slackClient.SendSlackMessage().Wait();
                    }

                    // checks for total missed slack messages during session, in the corresponding workspace of the token, in channels where the bot has been addded to
                    // Task.Result will block async code, and should be used carefully.
                    numberOfReceivedSlackMessages = slackClient.CheckReceivedSlackMessagesInWorkspace().Result;
                }

                // email

                // set dynamic automatic email reply message
                if (closedSession)
                {
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;
                    }
                    else
                    {
                        ReplyMessage = "\nThe recepient of this email is currently in a focused work session for another " + getSessionTime() + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
                    }
                }
                else {
                    if (CustomizedReplyMessageEnabled)
                    {
                        ReplyMessage = CustomizedReplyMessage;
                    }
                    else
                    {
                        ReplyMessage = "\nThe recepient of this email is currently in a focused work session for another " + getSessionTime() + " minutes, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
                    }
                }

                // this checks for missed emails and replies, adds replied emails to the list 'emailsReplied', which will be used at the end of the session to report on emails and then be emptied
                await CheckMail();
            }
        }

        private static async Task CheckMail()
        {
            // check mail and send an automatic reply if there was a new email.
            var unreadEmailsReceived = MsOfficeTracker.Helpers.Office365Api.GetInstance().GetUnreadEmailsReceived(DateTime.Now.Date);
            unreadEmailsReceived.Wait();
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
                }
            }
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

                // set control variable
                slackClientInitialized = true;

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
                        if (WindowFlaggerMessageBoxActive) { return; }
                        else
                        {
                            // show message box to ask if this is task-related
                            var selectedOption = MessageBox.Show("You opened a potentially distracting program during an active FocusSession. Do you want to read or reply to a message that is related to the task you are currently focussing on?", "Potentially distracting Program detected", MessageBoxButtons.YesNo);

                            // log the users answer
                            Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " and was shown the WindowFlagger Messagebox");

                            // set active MessageBox. We do not want to stack boxes, user will also not know anymore which box would have belonged to which application in the end if user would just let them stack
                            WindowFlaggerMessageBoxActive = true;

                            // check answer
                            // TODO store in database entry for study rather then just console-outprinting
                            if (selectedOption == DialogResult.Yes)

                            {
                                Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");

                                // log the users answer
                                Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");

                                // user responded to messagebox
                                WindowFlaggerMessageBoxActive = false;

                            }
                            else if (selectedOption == DialogResult.No)

                            {
                                Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

                                // log the users answer
                                Shared.Data.Database.GetInstance().LogInfo("WindowFlagger : The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

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
            internal async Task SendSlackMessage()
            {

                // send simple message to general channel and wait for the call to complete
                var channel = "#general";
                var text = "hello world";
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

                // get the list of all channels from that workspace
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