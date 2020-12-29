using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Shared;
using System.Linq;

namespace PersonalAnalytics.Views
{
    /// <summary>
    /// Interaction logic for LastStartWindow.xaml
    /// </summary>
    public partial class LastStartWindow : UserControl
    {
        public LastStartWindow(List<ITracker> trackers)
        {
            InitializeComponent();

            // show list of enabled trackers
            var enabledTrackers = trackers.Where(t => t.IsEnabled()).Select(t => t.Name).ToList();
            foreach (string tracker in enabledTrackers)
            {
                EnabledTrackerList.Inlines.Add("- " + tracker + "\n");
            }

            // show list of disabled trackers
            var disabledTrackers = trackers.Where(t => ! t.IsEnabled()).Select(t => t.Name).ToList();
            if (disabledTrackers.Count > 0)
            {
                foreach (string tracker in disabledTrackers)
                {
                    DisabledTrackerList.Inlines.Add("- " + tracker + "\n");
                }
            }
            else
            {
                DisabledTrackerList.Text = "No trackers disabled.";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this) as StartScreenContainer;
            w.EnableClose();
            w.Close();
            //Window.GetWindow(this).Close();
        }
    }
}
