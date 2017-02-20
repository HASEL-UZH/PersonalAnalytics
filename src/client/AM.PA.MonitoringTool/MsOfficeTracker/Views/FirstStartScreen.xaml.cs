using MsOfficeTracker.Helpers;
using Shared.Data;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MsOfficeTracker.Views
{
    /// <summary>
    /// Interaction logic for FirstStartScreen.xaml
    /// </summary>
    public partial class FirstStartScreen : System.Windows.Controls.UserControl
    {
        public FirstStartScreen()
        {
            InitializeComponent();
        }

        internal async void NextClicked()
        {
            if (Enable.IsChecked.HasValue)
            {
                if (Enable.IsChecked.Value)
                {
                    bool success = await Office365Api.GetInstance().Authenticate();

                    if (!success)
                    {
                        NotifyIcon notification = new NotifyIcon();
                        notification.Visible = true;
                        notification.BalloonTipTitle = "MsOfficeTracker disabled!";
                        notification.Icon = SystemIcons.Exclamation;
                        notification.Text = "The MsOfficeTracker was disabled as the authentication with Office 365 failed.";
                        notification.ShowBalloonTip(60 * 1000);
                    }

                    Leave(success);
                }
                else
                {
                    Leave(false);
                }
            }
            else
            {
                Leave(false);
            }
        }

        private void Leave(bool enabled)
        {
            Database.GetInstance().SetSettings("MsOfficeTrackerEnabled", enabled);
        }

        private void Enable_Checked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Visible;
        }

        private void Enable_Unchecked(object sender, RoutedEventArgs e)
        {
            ThanksMessage.Visibility = Visibility.Hidden;
        }
    }
}
