using System;
using System.Timers;
using System.Web;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FitbitTracker.View
{

    public partial class EmbeddedBrowser : UserControl
    {
        public delegate void OnFinish();
        public event OnFinish FinishEvent;

        public delegate void OnRegistrationToken(string token);
        public event OnRegistrationToken RegistrationTokenEvent;

        public delegate void OnError();
        public event OnError ErrorEvent;

        public EmbeddedBrowser(string url)
        {
            InitializeComponent();
            PABrowser.Navigate(url);
            PABrowser.Navigated += OnNavigation;
        }

        private void OnNavigation(object sender, NavigationEventArgs e)
        {
            if (!e.Uri.ToString().Equals(Settings.REGISTRATION_URL))
            {
                if (e.Uri.ToString().Contains("?code"))
                {
                    var queryDict = HttpUtility.ParseQueryString(e.Uri.Query);
                    string accessCode = queryDict.Get("code");
                    accessCode = accessCode.Replace("#_=_", "");
                    ShowTanksScreen();
                    RegistrationTokenEvent?.Invoke(accessCode);
                }
                else if (e.Uri.ToString().Contains("?error"))
                {
                    ShowErrorScreen();
                    ErrorEvent?.Invoke();
                }
            }
        }

        private void ShowErrorScreen()
        {
            PABrowser.Visibility = System.Windows.Visibility.Collapsed;
            Error.Visibility = System.Windows.Visibility.Visible;
            StartCountdown();
        }
        
        private void ShowTanksScreen()
        {
            PABrowser.Visibility = System.Windows.Visibility.Collapsed;
            Success.Visibility = System.Windows.Visibility.Visible;
            StartCountdown();
        }

        private void StartCountdown()
        {
            Timer timer = new Timer();
            timer.Interval = 10000;
            timer.Elapsed += Finished;
            timer.Start();
        }

        private void Finished(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Stop();
            FinishEvent?.Invoke();
        }
    }
}
