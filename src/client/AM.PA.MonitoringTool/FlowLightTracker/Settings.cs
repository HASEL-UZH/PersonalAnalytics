// Created by Manuela Zueger (2017-02-18) from the University of Zurich
// Created: 2015-10-30
// 
// Licensed under the MIT License.

namespace FlowLight
{
    internal class Settings
    {
        public const int UpdateInterval = 20; // in seconds
        public static bool IsEnabledByDefault = true; // TODO: change back
        public static bool IsAutomaticByDefault = true;
        public static bool IsDnDAllowedByDefault = true;
        public static int DefaultSensitivityLevel = 2;
        public static string[] DefaultBlacklist = { "skype", "lync" };
    }
}
