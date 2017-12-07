
namespace TimeSpentVisualizer.Models
{
    internal enum TimeSpentType
    {
        Meeting,
        File,
        Website,
        VsProject,
        CodeReview,
        Programs,
        Outlook
    }

    internal class TimeSpentItem
    {
        public TimeSpentItem(TimeSpentType type, string title, double duration)
        {
            Type = type;
            Title = title;
            DurationInMins = duration;
        }

        internal TimeSpentType Type { get; private set; }
        internal string Title { get; private set; }
        internal double DurationInMins { get; private set; }
    }
}
