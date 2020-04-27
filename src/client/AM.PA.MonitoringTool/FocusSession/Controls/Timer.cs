using System;
using System.Timers;

namespace FocusSession.Controls
{
    public class Timer
    {

        // https://www.codeproject.com/Articles/824887/How-To-List-The-Name-of-Current-Active-Window-in-C

        // private static System.Timers.Timer aTimer;  // timer functionality
        // setting them equal to control sessions (default value means equal). As soon as startTime is different it means a session is running
        private static DateTime startTime = new DateTime(2019, 2, 22, 14, 0, 0);    //just a default value, important is that is is equal to stopTime and in the past (so .now will never return the same value)
        private static DateTime stopTime = new DateTime(2019, 2, 22, 14, 0, 0);
        private static DateTime endTime = DateTime.Now;
        private static System.Timers.Timer aTimer;

        public static void CustomTimer()
        {

        }

        public static void StartTimer()
        {
            // there is no session currently running so we get the now. User is not meant to overwrite startTime randomly. In case of user clicks start button multiple times, nothing will happen.
            if (DateTime.Compare(startTime, stopTime) == 0)
            {
                startTime = DateTime.Now;

                // display a message to the user so the user gets feedback (important)
                System.Windows.Forms.MessageBox.Show("");
                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                System.Windows.Forms.MessageBox.Show("FocusSession started. Please make sure you have Windows Focus Assistant turned on.");
            }
        }
        public static void StopTimer()
        {
            // there is no session running so we do nothing
            if (DateTime.Compare(startTime, stopTime) == 0)
            {
                Console.WriteLine("no session running");
            }
            else
            {
                // get the current timestamp
                stopTime = DateTime.Now;
                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;
                Console.WriteLine(elapsedTime);
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

                // display a message to the user so the user gets feedback (important)
                System.Windows.Forms.MessageBox.Show("FocusSession stopped.");
                // workaround: calling twice because of 'splash screen dismisses dialog box' bug. More here https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-mes/5328590#5328590
                System.Windows.Forms.MessageBox.Show("You did focus for " + elapsedTime.Hours + " hours and " + elapsedTime.Minutes + " Minutes. Good job :)");
            }
        }


        public static void Countdown()
        {
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            startTime = DateTime.Now;
            //endTime = DateTime.Now.AddSeconds(10); //demonstration purposes
            endTime = DateTime.Now.AddMinutes(25); //Pomodo Timer

            SetTimer();
        }

        private static void SetTimer()
        {
            // Create a timer with a one second interval.
            aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (DateTime.Compare(DateTime.Now, endTime) > 0)
            {
                // get the current timestamp
                stopTime = DateTime.Now;
                // calculate the timespan
                TimeSpan elapsedTime = stopTime - startTime;
                // store in database
                Data.Queries.SaveTime(startTime, stopTime, elapsedTime);
                //reset. We set them equal to control sessions, this could never happen in a normal usecase.
                startTime = stopTime;

                Console.WriteLine("Timer elapsed");
                Console.WriteLine("The Timer elapsed at {0:HH:mm:ss.fff}",
                                  e.SignalTime);
                // stop the timer
                aTimer.Stop();
                aTimer.Dispose();

                // reset variable for context menu (in TrackerManager)
                Shared.Helpers.FocusSessionHelper._isRunningFocusSession = false;

                // display a message that counter run out. TODO maybe less intrusive way, this might interrupt the users workflow
                System.Windows.Forms.MessageBox.Show("FocusSession timer elapsed. Well done :)");
            }
            else
            {
                Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                                  e.SignalTime);
            }
        }
    }
}


