using MsOfficeTracker.Helpers;
using Shared;
using Shared.Data;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace FocusSession.Views
{
    /// <summary>
    /// Interaction logic for FirstStartScreen.xaml
    /// </summary>
    public partial class FirstStartScreen : System.Windows.Controls.UserControl, IFirstStartScreen
    {
        public FirstStartScreen()
        {
            InitializeComponent();
        }

        public string GetTitle()
        {
            return Settings.TrackerName;
        }

        public void PreviousClicked()
        {
            //not needed
        }

        public async void NextClicked()
        {
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, true);
            Leave(true);
        }

        private void Leave(bool enabled)
        {
            Database.GetInstance().SetSettings("FocusSessionTrackerEnabled", enabled);
        }
    }
}
