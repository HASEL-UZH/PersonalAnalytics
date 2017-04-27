// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Specialized;
using System.Windows.Media;
using Shared.Helpers;
using System.Windows.Documents;
using GoalSetting.Views;
using GoalSetting.Model;
using GoalSetting.Goals;
using System.Windows.Threading;
using System;

namespace GoalSetting
{
    /// <summary>
    /// Interaction logic for GoalSetting.xaml
    /// </summary>
    public partial class GoalSetting : UserControl
    {
        private ObservableCollection<Goal> _goals;
        private Goal _selectedGoal = null;

        public GoalSetting()
        {
            InitializeComponent();
            this._goals = GoalSettingManager.Instance.GetGoals();
            _goals.CollectionChanged += _rules_CollectionChanged;
            CheckRules.IsEnabled = _goals.Count > 0;

            LoadGoalStatus();
        }

        private void LoadGoalStatus()
        {
            RulesOverview.Children.Clear();

            if (_goals.Count > 0)
            {
                NoRulesText.Visibility = Visibility.Collapsed;
                RulesOverview.Visibility = Visibility.Visible;
            }
            else
            {
                NoRulesText.Visibility = Visibility.Visible;
                RulesOverview.Visibility = Visibility.Collapsed;
            }

            foreach (Goal goal in _goals)
            {
                goal.CalculateProgressStatus(false);
                
                StackPanel container = new StackPanel();
                container.MouseLeftButtonDown += Container_MouseLeftButtonDown;
                container.Tag = goal;
                container.Background = Shared.Settings.GrayColorBrush;
                container.Orientation = Orientation.Horizontal;
                Thickness containerMargin = container.Margin;
                container.Height = 60;
                containerMargin.Bottom = 5;
                container.Margin = containerMargin;

                Image smiley = new Image();
                smiley.Margin = new Thickness(10, 0, 10, 0);
                smiley.Height = 40;

                switch (goal.Progress.Status)
                {
                    case ProgressStatus.VeryLow:
                        smiley.Source = ImageHelper.BitmapToImageSource(Properties.Resources.smiley_5);
                        break;
                    case ProgressStatus.Low:
                        smiley.Source = ImageHelper.BitmapToImageSource(Properties.Resources.smiley_4);
                        break;
                    case ProgressStatus.Average:
                        smiley.Source = ImageHelper.BitmapToImageSource(Properties.Resources.smiley_3);
                        break;
                    case ProgressStatus.High:
                        smiley.Source = ImageHelper.BitmapToImageSource(Properties.Resources.smiley_2);
                        break;
                    case ProgressStatus.VeryHigh:
                        smiley.Source = ImageHelper.BitmapToImageSource(Properties.Resources.smiley_1);
                        break;
                }

                TextBlock text = new TextBlock();
                text.FontSize = 18;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Inlines.Add(goal.ToString());
                text.Inlines.Add(new LineBreak());
                text.Inlines.Add(goal.GetProgressMessage());

                Thickness margin = text.Margin;
                margin.Left = 20;
                text.Margin = margin;

                container.Children.Add(smiley);
                container.Children.Add(text);
                
                RulesOverview.Children.Add(container);
            }
        }

        private void Container_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach (StackPanel sp in RulesOverview.Children)
            {
                sp.Background = Shared.Settings.GrayColorBrush;
            }

            _selectedGoal = (Goal) (sender as StackPanel).Tag;
            (sender as StackPanel).Background = Shared.Settings.DarkGrayColorBrush;
            EditRule.IsEnabled = true;
            DeleteRule.IsEnabled = true;
        }

        private void _rules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckRules.IsEnabled = _goals.Count > 0;
            LoadGoalStatus();
        }

        private void CheckRules_Click(object sender, RoutedEventArgs e)
        {
            foreach (var goal in GoalSettingManager.Instance.GetGoals())
            {
                goal.CalculateProgressStatus(false);
            }
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                var popup = new GoalsPopUp(_goals);
                popup.ShowDialog();
            }));
        }
        
        private void AddRule_Click(object sender, RoutedEventArgs e)
        { 
            GoalSettingManager.Instance.AddNewGoal();
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGoal != null)
            {
                GoalSettingManager.Instance.DeleteGoal(_selectedGoal);
            }
            _selectedGoal = null;
            EditRule.IsEnabled = false;
            DeleteRule.IsEnabled = false;
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGoal != null)
            {
                UserControl controlToDisplay = null;

                switch (_selectedGoal.Rule.Goal)
                {
                    case RuleGoal.NumberOfEmailsInInbox:
                        controlToDisplay = new EmailInbox(_selectedGoal as GoalEmail);
                        break;

                    case RuleGoal.TimeSpentOn:
                        controlToDisplay = new TimeSpent(_selectedGoal as GoalActivity);
                        break;

                    case RuleGoal.NumberOfSwitchesTo:
                        controlToDisplay = new WorkFragmentation(_selectedGoal as GoalActivity);
                        break;
                }

                if (controlToDisplay != null)
                {
                    Window window = new Window
                    {
                        Content = controlToDisplay,
                        Title = "Edit: " + _selectedGoal.ToString(),
                        SizeToContent = SizeToContent.WidthAndHeight
                    };
                    window.ShowDialog();
                }
            }
            _selectedGoal = null;
            EditRule.IsEnabled = false;
            DeleteRule.IsEnabled = false;
        }

    }

}