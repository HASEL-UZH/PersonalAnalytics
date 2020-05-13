using Newtonsoft.Json;
using SlackAPI;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;


namespace FocusSession.Controls
{
    public class Timer
    {

        // setting them equal to control sessions (default value means equal). As soon as startTime is different it means a session is running
        private static DateTime startTime = new DateTime(2019, 2, 22, 14, 0, 0);    //just a default value, important is that is is equal to stopTime and in the past (so .now will never return the same value)
        private static DateTime stopTime = new DateTime(2019, 2, 22, 14, 0, 0);
        private static DateTime endTime = DateTime.Now;
        private static System.Timers.Timer aTimer;
        private static System.Collections.Generic.List<Microsoft.Graph.Message> emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>(); // this list is simply to keep track of the already replied to emails during the session
        private static String replyMessage = "Thank you for your Email. This is an automatically generated response by PersonalAnalytics. This mail-inbox is currently paused for a specific timeframe, after which your email will be received.";
        public static bool openSession { get; set; } = false;   // indicate if an openSession is running
        public static bool closedSession { get; set; } = false; // indicate if a closedSession is running


        // list of potentially distracting programs that we use for flagging check
        private static String[] windowFlaggerList = new string[3] { "Zulip", "Microsoft Teams", "Mozilla Thunderbird" };


        // for icon hover information
        public static TimeSpan getSessionTime()  // get the current session Time
        {
            if (openSession)
            {
                return DateTime.Now - startTime;    // return for how long the open session has been running
            }
            if (closedSession)
            {
                return endTime - startTime;         // return for how long the closed session will still be running
            }
            return TimeSpan.Zero;
        }

        // Session has been manually started by user. This is an open session.
        public static void StartOpenFocusSession()
        {
            // there is no session currently running so we get the now. User is not meant to overwrite startTime randomly. In case of user clicks start button multiple times, nothing will happen.
            if (!openSession && !closedSession)
            {

                // set startTime
                startTime = DateTime.Now;

                // update indicator
                openSession = true;

                // display a message to the user so the user gets feedback (important)
                System.Windows.Forms.MessageBox.Show("");

                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                System.Windows.Forms.MessageBox.Show("FocusSession started. Please make sure you have Windows Focus Assistant turned on.");

                // set the timer for email reply functionality
                SetTimer();
            }
        }

        // Session has been manually stopped by user
        public static void StopFocusSession()
        {
            if (openSession || closedSession)
            {

                // get the current timestamp
                stopTime = DateTime.Now;

                // indicate that session stopped
                openSession = false;
                closedSession = false;

                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;

                // store in database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime);

                //reset. We set them equal to control sessions, this could never happen in a normal usecase.
                startTime = stopTime;

                // stop if a timer is running
                if (aTimer != null && aTimer.Enabled)
                {
                    aTimer.Stop();
                    aTimer.Dispose();
                }

                String endMessage = "You did focus for " + elapsedTime.Hours + " hours and " + elapsedTime.Minutes + " Minutes. Good job :)";

                if (emailsReplied.Count > 0)
                {
                    endMessage += "During this Session, " + emailsReplied.Count + " Emails have been automatically replied.";
                }

                // display a message to the user so the user gets feedback (important)
                System.Windows.Forms.MessageBox.Show("FocusSession stopped.");

                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                System.Windows.Forms.MessageBox.Show(endMessage);

                // empty replied Emails list
                emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>();

            }
        }

        // user manually starts a closed session
        public static void StartClosedFocusSession()
        {
            // add start time
            startTime = DateTime.Now;

            // add the timeperiod, default is Pomodoro Timer 25 min, unless changed through the Settings
            endTime = DateTime.Now.AddMinutes(Settings.ClosedSessionDuration);

            // update indicator
            closedSession = true;

            // display a message to the user so the user gets feedback (important)
            System.Windows.Forms.MessageBox.Show("");

            // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
            System.Windows.Forms.MessageBox.Show("FocusSession started for " + Settings.ClosedSessionDuration + " min. Please make sure you have Windows Focus Assistant turned on.");

            // set the timer
            SetTimer();
        }

        private static void SetTimer()
        {
            // 10 sec interval, checking and replying to emails or ending session
            aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // check if this is a closed session where a timer actually runs out, and if we hit the endTime already
            if (closedSession && DateTime.Compare(DateTime.Now, endTime) > 0)
            {
                // get the current timestamp
                stopTime = DateTime.Now;

                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;

                // store in database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime);

                //reset. We set them equal to control sessions, this could never happen in a normal usecase.
                startTime = stopTime;

                // stop the timer
                aTimer.Stop();
                aTimer.Dispose();

                // update indicator
                closedSession = false;

                // display a message that counter run out. TODO maybe less intrusive way, this might interrupt the users workflow
                System.Windows.Forms.MessageBox.Show("FocusSession timer elapsed. Well done :)");

                // empty replied Emails list
                emailsReplied = new System.Collections.Generic.List<Microsoft.Graph.Message>();

            }
            else
            {

                if (System.IO.File.Exists(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json")))
                {
                    // deserialized config.json to fetch tokens from class
                    string allText = System.IO.File.ReadAllText(Path.Combine(Shared.Settings.ExportFilePath, @"SlackConfig.json"));
                    Configuration.SlackConfig slackConfig = JsonConvert.DeserializeObject<Configuration.SlackConfig>(allText);

                    // test sending message to slack via FocusSession Bot
                    // does an asynchronous call mess up the messagebox flagging?
                    var p = new Async();
                    p.SendSlackMessage(slackConfig.botAuthToken).Wait();

                }

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
                            //TODO add reply call to office365Api, call it here (with the email.From.EmailAddress as reply parameter?)
                            // if ReplyTo is speficied, per RFC 2822 we should send it to that address, otherwise to the from address
                            await MsOfficeTracker.Helpers.Office365Api.GetInstance().SendReplyEmail(email.Id, email.From.EmailAddress.Name, email.From.EmailAddress.Address, replyMessage);

                            //add email to list of already replied emails during this focus session
                            emailsReplied.Add(email);
                        }
                    }
                }
            }
        }

        // this method is calles by the WindowsActivityTracker Demon, upon a foreground window/program switch, in case of an active FocusSession running
        // it checks if it is a potentially distracting program according to the list, currently printing to the Console
        public static void WindowFlagger(String currentWindowTitle)
        {

            foreach (String windowFlagger in windowFlaggerList)
                if (currentWindowTitle.Contains(windowFlagger))
                {
                    // show message box to ask if this is task-related
                    var selectedOption = MessageBox.Show("You opened a potentially distracting program during an active FocusSession. Do you want to read or reply to a message that is related to the task you are currently focussing on?", "Potentially distracting Program detected", MessageBoxButtons.YesNo);

                    // check answer
                    // TODO store in database entry for study rather then just console-outprinting
                    if (selectedOption == DialogResult.Yes)

                    {

                        Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is task-related");


                    }

                    else if (selectedOption == DialogResult.No)

                    {

                        Console.WriteLine("The participant opened " + currentWindowTitle + " to read or reply to a message that is not task-related");

                    }
                }

        }

        class Async
        {
            public async Task SendSlackMessage(string token)
            {
                // instantiate a new Slack Client by provding a token
                var client = new SlackTaskClient(token);

                // send simple message to channel and wait for the call to complete
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
        }

    }
}