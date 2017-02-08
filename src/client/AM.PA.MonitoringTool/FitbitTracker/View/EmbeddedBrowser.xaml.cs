// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-27
// 
// Licensed under the MIT License.

using System;
using System.Timers;
using System.Web;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FitbitTracker.View
{

    //Browser view that is used to retrieve access tokens from fitbit
    public partial class EmbeddedBrowser : UserControl
    {
        private bool embedded;

        //Called when process is finished
        public delegate void OnFinish();
        public event OnFinish FinishEvent;

        //Called when new tokens were received
        public delegate void OnRegistrationToken(string token);
        public event OnRegistrationToken RegistrationTokenEvent;

        //Called when an error happened during retrieving new tokens
        public delegate void OnError();
        public event OnError ErrorEvent;

        public EmbeddedBrowser()
        {
            embedded = true;
            InitializeComponent();
            PABrowser.Navigated += OnNavigation;
        }

        public EmbeddedBrowser(string url)
        {
            embedded = false;
            InitializeComponent();
            PABrowser.Navigate(url);
            PABrowser.Navigated += OnNavigation;
        }

        public void Navigate(string url)
        {
            PABrowser.Navigate(url);
        }

        //Called when navigation to a new URL is completed. Here we have to check the code parameter in the URL. It contains the first access token. If an error parameter is passed in the URL, we know that retrieving tokens failed.
        private void OnNavigation(object sender, NavigationEventArgs e)
        {
            if (!e.Uri.ToString().Equals(Settings.REGISTRATION_URL))
            {
                if (e.Uri.ToString().Contains("?code"))
                {
                    var queryDict = HttpUtility.ParseQueryString(e.Uri.Query);
                    string accessCode = queryDict.Get("code");
                    accessCode = accessCode.Replace("#_=_", "");
                    if (!embedded)
                    {
                        ShowThanksScreen();
                    }
                    RegistrationTokenEvent?.Invoke(accessCode);
                }
                else if (e.Uri.ToString().Contains("?error"))
                {
                    if (!embedded)
                    {
                        ShowErrorScreen();
                    }
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
        
        private void ShowThanksScreen()
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