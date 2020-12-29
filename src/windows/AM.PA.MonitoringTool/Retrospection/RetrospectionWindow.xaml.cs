using Shared;
using Shared.Data;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Retrospection
{
    public partial class RetrospectionWindow : Window
    {
        System.Windows.Forms.WebBrowser webBrowser;
        string _currentPage;
        VisType _currentVisType;

        public RetrospectionWindow(bool hideFeedback, bool hideAbout)
        {
            InitializeComponent();
            webBrowser = (wbWinForms.Child as System.Windows.Forms.WebBrowser);

            FeedbackInfo.Visibility = (hideFeedback) ? Visibility.Collapsed : Visibility.Visible;
            AboutInfo.Visibility = (hideAbout) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            //var stream = OnStats();
            //todo: path issues: http://stackoverflow.com/questions/27661986/include-static-js-and-css-webbrowser-control
            //webBrowser.NavigateToString(stream);

#if !DEBUG
    webBrowser.ScriptErrorsSuppressed = true;
#endif

        webBrowser.Navigating += (o, ex) =>
        {
            ShowLoading(true);
        };

        webBrowser.Navigated += (o, ex) =>
        {
            ShowLoading(false);

#if DEBUG
            webBrowser.Document.Window.Error += (w, we) =>
            {
                we.Handled = true;
                Logger.WriteToConsole(string.Format(CultureInfo.InvariantCulture, "# URL: {1}, LN: {0}, ERROR: {2}", we.LineNumber, we.Url, we.Description));
            };    
#endif
        };

        webBrowser.IsWebBrowserContextMenuEnabled = false;
        webBrowser.ObjectForScripting = new ObjectForScriptingHelper(); // allows to use javascript to call functions in this class
        webBrowser.WebBrowserShortcutsEnabled = false;
        webBrowser.AllowWebBrowserDrop = false;


        // load default page
        WebBrowserNavigateTo(Handler.GetInstance().GetDashboardHome());
        SwitchToWeekButton.Visibility = Visibility.Visible;
        SwitchToDayButton.Visibility = Visibility.Collapsed;
        }

        private void PrivacyStatement_Clicked(object sender, RoutedEventArgs e)
        {     
            Handler.GetInstance().OpenPrivacyStatement();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {  
            Database.GetInstance().LogInfo("Retrospection closed");  
        }

        /// <summary>
        /// Not closing the window, as it cannot be opened again
        /// Just hide it, and then show it again.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void ShowLoading(bool isLoading)
        {
            if (isLoading)
            {
                PlusOneButton.IsEnabled = false;
                TodayButton.IsEnabled = false;
                MinusOneButton.IsEnabled = false;
                DatePicker.IsEnabled = false;
                SwitchToWeekButton.IsEnabled = false;
                SwitchToDayButton.IsEnabled = false;
                wbWinForms.Visibility = Visibility.Collapsed;
                LoadingSign.Visibility = Visibility.Visible;
            }
            else
            {
                LoadingSign.Visibility = Visibility.Collapsed;
                wbWinForms.Visibility = Visibility.Visible;
                PlusOneButton.IsEnabled = true;
                TodayButton.IsEnabled = true;
                MinusOneButton.IsEnabled = true;
                DatePicker.IsEnabled = true;
                SwitchToWeekButton.IsEnabled = true;
                SwitchToDayButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Navigate the browser to the url in case the web browser is live, 
        /// the url is ready and the same is not the same
        /// </summary>
        /// <param name="parameters"></param>
        private void WebBrowserNavigateTo(string url, bool navigateEnforced = false)
        {
            if (webBrowser == null || url == null) return;

            if (_currentPage != url || navigateEnforced == true)
            {
                _currentPage = url;
                webBrowser.Navigate(url);
                Database.GetInstance().LogInfo("Retrospection, navigated to: " + url);
            }
        }

        /// <summary>
        /// Update the window title according to the website header
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wbWinForms_DocumentTitleChanged(object sender, EventArgs e)
        {
            this.RetrospectionTitle.Text = (sender as System.Windows.Forms.WebBrowser).DocumentTitle;
            //this.Title = (sender as System.Windows.Forms.WebBrowser).DocumentTitle;
        }

        #region Day & Week Navigation

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as DatePicker;
            DateTime? date = picker.SelectedDate;
            if (date != null && date.Value != null)
            {
                var selectedDate = date.Value;
                if (selectedDate > DateTime.Now)
                {
                    selectedDate = DateTime.Now;
                    DatePickerSelectDate(selectedDate);
                }

                //todo: check if dates are available before navigating
                WebBrowserNavigateTo(Handler.GetInstance().GetDashboardNavigateUriForType(date.Value, _currentVisType));
            }
        }

        private void DatePickerSelectDate(DateTime date)
        {
            DatePicker.SelectedDate = date;
        }

        private void Today_Clicked(object sender, EventArgs e)
        {
            DatePickerSelectDate(DateTime.Now);
        }

        private void MinusOne_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentVisType == VisType.Day)
                {
                    var minusOneDay = DatePicker.SelectedDate.Value.AddDays(-1);
                    DatePickerSelectDate(minusOneDay);
                }
                else if (_currentVisType == VisType.Week)
                {
                    var minusOneWeek = DatePicker.SelectedDate.Value.AddDays(-7);
                    DatePickerSelectDate(minusOneWeek);
                }
            }
            catch { }
        }

        private void PlusOne_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentVisType == VisType.Day)
                {
                    var plusOneDay = DatePicker.SelectedDate.Value.AddDays(+1);
                    DatePickerSelectDate(plusOneDay);
                }
                else if (_currentVisType == VisType.Week)
                {
                    var plusOneWeek = DatePicker.SelectedDate.Value.AddDays(+7);
                    DatePickerSelectDate(plusOneWeek);
                }
            }
            catch { }
        }

        private void SwitchToDay_Clicked(object sender, RoutedEventArgs e)
        {
            _currentVisType = VisType.Day;
            SwitchToWeekButton.Visibility = Visibility.Visible;
            SwitchToDayButton.Visibility = Visibility.Collapsed;
            TodayButton.Content = "Today";
            WebBrowserNavigateTo(Handler.GetInstance().GetDashboardNavigateUriForType(DatePicker.SelectedDate.Value, _currentVisType));
        }

        private void SwitchToWeek_Clicked(object sender, RoutedEventArgs e)
        {
            _currentVisType = VisType.Week;
            SwitchToWeekButton.Visibility = Visibility.Collapsed;
            SwitchToDayButton.Visibility = Visibility.Visible;
            TodayButton.Content = "This Week";
            WebBrowserNavigateTo(Handler.GetInstance().GetDashboardNavigateUriForType(DatePicker.SelectedDate.Value, _currentVisType));
        }

        #endregion

        #region Other Options

        public void ForceRefreshWindow()
        {
            //TODO: only force refresh today if the data is more than 5 minutes old

            //if (DatePicker.SelectedDate.HasValue && DatePicker.SelectedDate.Value.Date == DateTime.Now.Date)
            _currentPage = Handler.GetInstance().GetDashboardNavigateUriForType(DateTime.MinValue, _currentVisType); // to force refresh
            DatePickerSelectDate(DateTime.Now);
        }

        private void Feedback_Clicked(object sender, EventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }

        //private void Settings_Clicked(object sender, EventArgs e)
        //{
        //    Handler.GetInstance().OpenSettings();
        //}

        private void Refresh_Clicked(object sender, EventArgs e)
        {
            WebBrowserNavigateTo(_currentPage, true);
        }

        private void About_Clicked(object sender, RoutedEventArgs e)
        {
            Handler.GetInstance().OpenAbout();
        }

        #endregion
    }
}
