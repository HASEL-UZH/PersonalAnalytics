// Created by Andre Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.


namespace Retrospection
{
    public class Settings
    {
#if PilotManu_March17
        public static bool IsEnabled = false;
#elif Pilot_TaskDetection_March17
        public static bool IsEnabled = false;
#else
        public static bool IsEnabled = true;
#endif
    }
}
