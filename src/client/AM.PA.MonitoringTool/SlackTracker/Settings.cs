using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker
{
    internal class Settings
    {
        internal const string Name = "Slack Tracker";
        internal const string TRACKER_ENEABLED_SETTING = "SlackTrackerEnabled";

        internal static readonly bool IsEnabledByDefault = true;
        internal static readonly bool IsDetailedCollectionEnabled = false; // default: disabled
    }
}
