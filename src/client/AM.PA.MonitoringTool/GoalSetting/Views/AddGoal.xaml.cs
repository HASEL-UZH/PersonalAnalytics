// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-09
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using GoalSetting.Goals;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for AddRule.xaml
    /// </summary>
    public partial class AddGoal : UserControl
    {
        private ObservableCollection<Goal> goals;

        public AddGoal(ObservableCollection<Goal> goals)
        {
            this.goals = goals;
            InitializeComponent();
        }
        
        public void Close()
        {
            (this.Parent as Window).Close();
        }

        private void WorkFragmentation_Click(object sender, RoutedEventArgs e)
        {
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Visible;
            Step2.Content = new WorkFragmentation(this);
        }

        private void TimeSpent_Click(object sender, RoutedEventArgs e)
        {
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Visible;
            Step2.Content = new TimeSpent(this);
        }

        private void Emails_Click(object sender, RoutedEventArgs e)
        {
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Visible;
            Step2.Content = new EmailInbox(this);
        }
    }

}