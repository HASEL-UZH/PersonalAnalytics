using System;
using System.Timers;

namespace FocusSession.Controls
{
    public class Timer
    {

        //private static System.Timers.Timer aTimer;  // timer functionality
        // setting them equal to control sessions (default value means equal). As soon as startTime is different it means a session is running
        private static DateTime startTime = DateTime.Now;
        private static DateTime stopTime = DateTime.Now;

        public static void StartTimer()
        {
            // there is no session currently running so we get the now. User is not meant to overwrite startTime randomly.
            if (DateTime.Compare(startTime, stopTime) == 0)
            {
                startTime = DateTime.Now;
            }
        }
        public static void StopTimer()
        {
            //there is no session running so we do nothing
            if (DateTime.Compare(startTime, stopTime) == 0)
            {
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

        /*
         * TIMER FUNCTIONALITY
         * 
         * 
        public static void Main()
        {
            SetTimer();

            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            startTime = DateTime.Now;
            aTimer.Stop();
            aTimer.Dispose();

            Console.WriteLine("Terminating the application...");
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
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
        }

    */
    }
}


