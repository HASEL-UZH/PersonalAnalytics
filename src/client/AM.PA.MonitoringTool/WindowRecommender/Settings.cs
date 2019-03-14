namespace WindowRecommender
{
    internal static class Settings
    {
        internal const bool IsEnabledByDefault = true;
        internal const string EnabledSettingDatabaseKey = "WindowRecommenderEnabled";
        internal const string EventTable = "window_recommender_events";

        internal const int OverlayAlpha = 64;
        internal const int FramesPerSecond = 10;

        internal const int NumberOfWindows = 3;

        internal const int DurationIntervalSeconds = 10;
        internal const int DurationTimeframeMinutes = 10;

        internal const int FrequencyIntervalSeconds = 10;
        internal const int FrequencyTimeframeMinutes = 10;
    }
}
