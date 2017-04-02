// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using GoalSetting.Rules;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;
using Shared.Helpers;
using System.Windows.Documents;

namespace GoalSetting
{
    /// <summary>
    /// Interaction logic for GoalSetting.xaml
    /// </summary>
    public partial class GoalSetting : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<PARule> _rules;
        private bool _hasChanged = false;
        
        public bool HasChanged { get { return _hasChanged; } set { _hasChanged = value; NotifyPropertyChanged("HasChanged"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public GoalSetting(ObservableCollection<PARule> rules)
        {
            InitializeComponent();
            this._rules = rules;
            Rules.SelectionMode = DataGridSelectionMode.Single;
            Rules.ItemsSource = rules;
            _rules.CollectionChanged += _rules_CollectionChanged;
            CheckRules.IsEnabled = _rules.Count > 0;
            SaveRules.DataContext = this;

            LoadRuleStatus();
        }

        private void LoadRuleStatus()
        {
            foreach (PARule rule in _rules)
            {
                rule.CalculateProgressStatus();

                StackPanel container = new StackPanel();
                container.Tag = rule;
                container.Background = new SolidColorBrush(Colors.LightGray);
                container.Orientation = Orientation.Horizontal;
                Thickness containerMargin = container.Margin;
                container.Height = 60;
                containerMargin.Bottom = 5;
                container.Margin = containerMargin;

                Image smiley = new Image();
                smiley.Margin = new Thickness(10, 0, 10, 0);
                smiley.Height = 40;

                switch (rule.Progress.Status)
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
                text.Inlines.Add(rule.ToString());
                text.Inlines.Add(new LineBreak());
                text.Inlines.Add(rule.GetProgressMessage());

                Thickness margin = text.Margin;
                margin.Left = 20;
                text.Margin = margin;

                container.Children.Add(smiley);
                container.Children.Add(text);
                
                RulesOverview.Children.Add(container);
            }
        }

        private void _rules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckRules.IsEnabled = _rules.Count > 0;
        }

        private void CheckRules_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.CheckRules(_rules);
        }

        private void SaveRules_Click(object sender, RoutedEventArgs e)
        {
            HasChanged = ! DatabaseConnector.SaveRules(_rules);
        }

        private void AddRule_Click(object sender, RoutedEventArgs e)
        { 
            GoalSettingManager.Instance.AddNewRule();
            HasChanged = true;
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            PARule rule = (PARule) Rules.SelectedItem;
            GoalSettingManager.Instance.DeleteRule(rule);
            HasChanged = true;
        }

        private void Rules_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DeleteRule.IsEnabled = Rules.SelectedCells.Count > 0;
            EditRule.IsEnabled = Rules.SelectedCells.Count > 0;
        }

        private void EditRule_Click(object sender, RoutedEventArgs e)
        {
            //TODO
        }
    }

}