using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLight
{
    internal class Settings
    {
        public const int UpdateInterval = 20; // in seconds
        public static bool IsEnabledByDefault = true;
        public static bool IsAutomaticByDefault = true;
        public static bool IsDnDAllowedByDefault = true;
        public static int DefaultSensitivityLevel = 2;
        public static string[] DefaultBlacklist = { "skype", "lync" };
    }
}
