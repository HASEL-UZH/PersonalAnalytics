// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

namespace FocusSession
{
    public static class Settings
    {
        public const string TrackerName = "FocusSession Tracker";
        public static bool IsEnabledByDefault = true;
        public static bool IsDisabledByDefault = false;
        public static string IsTextMessageByDefault = "\nThe recepient of this email is currently in a focused work session, and will receive your message after completing the current task. \nThis is an automatically generated response by the FocusSession-Extension of the PersonalAnalytics Tool https://github.com/Phhofm/PersonalAnalytics. \n";
        public static string IsTextListByDefault = "Skype, WhatsApp, Zoom, Microsoft Outlook, Google Hangouts, Discord, LINE, Signal, Trilian, Viber, Pidgin, eM Client, Thunderbird, Whatsapp Web, Facebook, Winmail, Telegram, Yahoo Mail, Camfrog, Messenger, TextNow, Slack, mIRC, BlueMail, Paltalk, Mailbird, Jisti, Jabber, OpenTalk, ICQ, Gmail, Tango, Lync, Pegasus, Mailspring, Teamspeak, QuizUp, IGA, Zello, Jelly SMS, Mammail, Line, MSN, inSpeak, Spark, TorChat, ChatBox, AIM, HexChat, HydraIRC, Mulberry, Claws Mail, Pandion, ZChat, Franz, Microsoft Teams, Zulip";
        public const string FocusTimerTable = "focusTimer";
        public const string FocusTimerSessionDuration = "timeDuration";
        public static int ClosedSessionDuration = 20;
        internal const string CUSTOMIZEDTIMERDURATION_INT_SETTING = "ClosedSessionDuration";
        internal const string REPLYMESSAGE_ENEABLED_SETTING = "ReplyMessageEnabled";
        internal const string WINDOWFLAGGING_ENEABLED_SETTING = "WindowFlaggingEnabled";
        internal const string CUSTOMIZEDREPLYMESSAGE_ENEABLED_SETTING = "CustomizedReplyMessageEnabled";
        internal const string CUSTOMIZEDREPLYMESSAGE_TEXT_SETTING = "CustomizedReplyMessage";
        internal const string CUSTOMIZEDFLAGGINGLIST_ENEABLED_SETTING = "CustomizedFlaggingListEnabled";
        internal const string CUSTOMIZEDFLAGGINGLIST_TEXT_SETTING = "CustomizedFlaggingList";
    }
}
