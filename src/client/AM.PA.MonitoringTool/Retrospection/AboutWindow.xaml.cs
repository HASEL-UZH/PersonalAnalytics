using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Retrospection
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(string toolVersion, int dbVersion)
        {
            InitializeComponent();
            TbVersion.Text = "Tool-Version: " + toolVersion + ", Database-Version: " + dbVersion;
            PrivacyStatementUriText.Text = Shared.Settings.PrivacyStatementUri;
        }

        private void Feedback_Clicked(object sender, RoutedEventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }

        private void Andre_Clicked(object sender, MouseButtonEventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }

        private void Tom_Clicked(object sender, MouseButtonEventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }

        private void Thomas_Clicked(object sender, MouseButtonEventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void PrivacyStatement_Clicked(object sender, MouseButtonEventArgs e)

        {
            Handler.GetInstance().OpenPrivacyStatement();
        }

        //private void CheckForUpdates_Clicked(object sender, RoutedEventArgs e)
        //{
        //    // todo: implement
        //}
    }
}
