// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-09
// 
// Licensed under the MIT License.

using GoalSetting.Rules;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using GoalSetting.Model;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for AddRule.xaml
    /// </summary>
    public partial class AddRule : UserControl
    {
        private ObservableCollection<PARule> rules;
        private GoalDomain selectedGoalDomain;

        public AddRule(ObservableCollection<PARule> rules)
        {
            this.rules = rules;
            InitializeComponent();
        }

        private void Step_2(GoalDomain goalDomain) { 
            Step1.Visibility = Visibility.Collapsed;
            selectedGoalDomain = goalDomain;

            TextBlock title = new TextBlock();
            title.FontSize = 18;
            title.Inlines.Add("Choose your work behaviour goal");
            Step2.Children.Add(title);

            Button workFragmentationButton = new Button() { Content = "Work Fragmentation" };
            workFragmentationButton.Click += WorkFragmentationButton_Click;
            workFragmentationButton.Height = 40;
            workFragmentationButton.Margin = new Thickness(25, 10, 25, 10);

            Button timeSpentButton = new Button() { Content = "Time spent on" };
            timeSpentButton.Click += TimeSpentButton_Click;
            timeSpentButton.Height = 40;
            timeSpentButton.Margin = new Thickness(25, 10, 25, 10);

            Button emailsInboxButton = new Button() { Content = "Emails in Inbox" };
            emailsInboxButton.Click += EmailsInboxButton_Click;
            emailsInboxButton.Height = 40;
            emailsInboxButton.Margin = new Thickness(25, 10, 25, 10);

            switch (goalDomain)
            {
                case GoalDomain.Focus:
                case GoalDomain.Break:
                    NotYetSupported.Visibility = Visibility.Visible;
                    break;

                case GoalDomain.Browsing:
                case GoalDomain.Coding:
                case GoalDomain.Meetings:
                    Step2.Children.Add(workFragmentationButton);
                    Step2.Children.Add(timeSpentButton);
                    Step2.Visibility = Visibility.Visible;
                    break;

                case GoalDomain.Emails:
                    Step2.Children.Add(workFragmentationButton);
                    Step2.Children.Add(timeSpentButton);
                    Step2.Children.Add(emailsInboxButton);
                    Step2.Visibility = Visibility.Visible;
                    break;
            }
            
        }

        private void EmailsInboxButton_Click(object sender, RoutedEventArgs e)
        {
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Visible;
            Step3.Content = new EmailInbox(this);
        }

        private void TimeSpentButton_Click(object sender, RoutedEventArgs e)
        {
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Visible;
            Step3.Content = new TimeSpent(selectedGoalDomain, this);
        }

        private void WorkFragmentationButton_Click(object sender, RoutedEventArgs e)
        {
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Visible;
            Step3.Content = new WorkFragmentation(selectedGoalDomain, this);
        }

        private void Step_2_Email(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Emails);
        }

        private void Step_2_Coding(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Coding);
        }

        private void Step_2_Focus(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Focus);
        }

        private void Step_2_Browsing(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Browsing);
        }

        private void Step_2_Breaks(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Break);
        }

        private void Step_2_Meetings(object sender, RoutedEventArgs e)
        {
            Step_2(GoalDomain.Meetings);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        public void Close()
        {
            (this.Parent as Window).Close();
        }

    }

}