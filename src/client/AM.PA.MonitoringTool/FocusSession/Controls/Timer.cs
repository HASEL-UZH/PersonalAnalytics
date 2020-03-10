using System;
using System.Timers;

namespace FocusSession.Controls
{
    public class Timer
    {

        // private static System.Timers.Timer aTimer;  // timer functionality
        // setting them equal to control sessions (default value means equal). As soon as startTime is different it means a session is running
        private static DateTime startTime = new DateTime(2019, 2, 22, 14, 0, 0);    //just a default value, important is that is is equal to stopTime and in the past (so .now will never return the same value)
        private static DateTime stopTime = new DateTime(2019, 2, 22, 14, 0, 0);
        private static DateTime endTime = DateTime.Now;
        private static System.Timers.Timer aTimer;

        public static void StartTimer()
        {
            // there is no session currently running so we get the now. User is not meant to overwrite startTime randomly. In case of user clicks start button multiple times, nothing will happen.
            if (DateTime.Compare(startTime, stopTime) == 0)
            {
                startTime = DateTime.Now;
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
            }
        }


        public static void Countdown()
        {
            SetTimer();

            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            startTime = DateTime.Now;
            endTime = DateTime.Now.AddSeconds(10);
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
                Console.WriteLine("Timer elapsed");
                Console.WriteLine("The Timer elapsed at {0:HH:mm:ss.fff}",
                                  e.SignalTime);
                aTimer.Stop();
                aTimer.Dispose();
            }
            else
            {
                Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                                  e.SignalTime);
            }
        }
    }
}


