using Shared;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for StartScreenContainer.xaml
    /// </summary>
    public partial class StartScreenContainer : Window
    {
        private string _appVersion;
        private List<FirstStartScreenContainer> startScreens;
        private int shownScreen = 0;
        private List<string> trackerNames = new List<string>();

        public StartScreenContainer(string version, List<FirstStartScreenContainer> startScreens)
        {
            InitializeComponent();
            _appVersion = version;
            this.startScreens = startScreens;
            TbVersion.Text = "Version: " + _appVersion;

            Top.Inlines.Add(startScreens[0].Title);
            Content.Children.Add(startScreens[0].Content);
        }
        
        private void NextClicked(object sender, RoutedEventArgs args)
        {
            startScreens[shownScreen].NextCallback?.Invoke();
            ShowNextScreen();
        }

        private void BackClicked(object sender, RoutedEventArgs args)
        {
            startScreens[shownScreen].PreviousCallback?.Invoke();
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
                Top.Inlines.Add(startScreens[shownScreen].Title);
                Content.Children.Clear();
                Content.Children.Add(startScreens[shownScreen].Content);

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
                Top.Inlines.Add(startScreens[shownScreen].Title);
                if (!trackerNames.Contains(startScreens[shownScreen].Title))
                {
                    trackerNames.Add(startScreens[shownScreen].Title);
                }

                Content.Children.Clear();
                Content.Children.Add(startScreens[shownScreen].Content);
            }
        }

        private void ShowLastScreen()
        {
            Top.Inlines.Clear();
            Top.Inlines.Add("Personal Analytics is now ready to be used!");
            Content.Children.Clear();
            Content.Children.Add(new LastStartWindow(trackerNames));

            Buttons.Visibility = Visibility.Hidden;
        }
    }
}