// Created by André Meyer at University of Zurich
// Created: 2016-04-22
// 
// Licensed under the MIT License.
using System.Windows;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : Window
    {
        private string _appVersion;
        public FirstStartWindow(string version)
        {
            InitializeComponent();
            _appVersion = version;
            TbVersion.Text = "Version: " + _appVersion;
        }

        private void NextClicked(object sender, RoutedEventArgs e)
        {
            // currently: don't do anything else
            DialogResult = true;
            this.Close();
        }

        private void Feedback_Clicked(object sender, RoutedEventArgs e)
        {
            Shared.Helpers.FeedbackHelper.SendFeedback("", "", _appVersion);
        }
    }
}
