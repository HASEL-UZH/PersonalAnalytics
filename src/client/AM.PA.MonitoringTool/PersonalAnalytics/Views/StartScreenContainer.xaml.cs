// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-07
// 
// Licensed under the MIT License.

using Shared;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Windows.Controls;

namespace PersonalAnalytics.Views
{
    /// <summary>
    /// Interaction logic for StartScreenContainer.xaml
    /// </summary>
    public partial class StartScreenContainer : Window
    {
        private string _appVersion;
        private List<IFirstStartScreen> startScreens;
        private int shownScreen = 0;
        private bool _canClose = false;

        public StartScreenContainer(string version, List<IFirstStartScreen> startScreens)
        {
            InitializeComponent();
            //Prevent this window from being closed.
            this.Closing += new System.ComponentModel.CancelEventHandler(OnClose);

            _appVersion = version;
            this.startScreens = startScreens;
            TbVersion.Text = "Version: " + _appVersion;

            Top.Inlines.Add(startScreens[0].GetTitle());
            Content.Children.Add((UserControl) startScreens[0]);
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_canClose;
        }

        public void EnableClose()
        {
            _canClose = true;
        }

        private void NextClicked(object sender, RoutedEventArgs args)
        {
            startScreens[shownScreen].NextClicked();
            ShowNextScreen();
        }

        private void BackClicked(object sender, RoutedEventArgs args)
        {
            startScreens[shownScreen].PreviousClicked();
            ShowPreviousScreen();
        }
        
        private void Feedback_Clicked(object sender, RoutedEventArgs args)
        {
            Shared.Helpers.FeedbackHelper.SendFeedback("", "", _appVersion);
        }

        private void ShowPreviousScreen()
        {
            shownScreen--;
            if (shownScreen >= 0)
            {
                Top.Inlines.Clear();
                Top.Inlines.Add(startScreens[shownScreen].GetTitle());
                Content.Children.Clear();
                Content.Children.Add((UserControl) startScreens[shownScreen]);

                if (shownScreen == 0)
                {
                    Back.IsEnabled = false;
                }
            }
        }

        private void ShowNextScreen()
        {
            shownScreen++;
            Back.IsEnabled = true;

            if (shownScreen > startScreens.Count - 1)
            {
                ShowLastScreen();
            }
            else
            {
                Top.Inlines.Clear();
                Top.Inlines.Add(startScreens[shownScreen].GetTitle());

                Content.Children.Clear();
                Content.Children.Add((UserControl) startScreens[shownScreen]);
            }
        }

        private void ShowLastScreen()
        {
            Top.Inlines.Clear();
            Top.Inlines.Add("Personal Analytics is now ready to be used!");
            Content.Children.Clear();
            Content.Children.Add(new LastStartWindow(TrackerManager.GetInstance().GetTrackers()));

            Buttons.Visibility = Visibility.Hidden;
        }
    }
}