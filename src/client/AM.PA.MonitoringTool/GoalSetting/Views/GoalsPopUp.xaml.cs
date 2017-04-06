using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Documents;
using Shared;
using Shared.Helpers;
using GoalSetting.Goals;
using GoalSetting.Model;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for RulePopUp.xaml
    /// </summary>
    public partial class GoalsPopUp : Window
    {
        private ObservableCollection<Goal> goals;

        public GoalsPopUp(ObservableCollection<Goal> goals)
        {
            InitializeComponent();
            this.goals = goals;
            AddHeaderPictures();
            AddGoals();
        }

        internal void AddHeaderPictures()
        {
            Close.Source = ImageHelper.BitmapToImageSource(Properties.Resources.close);
            Dashboard.Source = ImageHelper.BitmapToImageSource(Properties.Resources.dashboard);
        }

        internal void AddGoals()
        {
            foreach (Goal goal in goals)
            {
                StackPanel container = new StackPanel();
                container.Tag = goal;
                container.Background = new SolidColorBrush(Colors.LightGray);
                container.Orientation = System.Windows.Controls.Orientation.Horizontal;
                Thickness containerMargin = container.Margin;
                container.Height = 30;
                containerMargin.Bottom = 10;
                container.Margin = containerMargin;

                System.Windows.Controls.Image smiley = new System.Windows.Controls.Image();
                smiley.Height = 25;

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
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Inlines.Add(goal.ToString());
                text.Inlines.Add(new LineBreak());
                text.Inlines.Add(goal.GetProgressMessage());

                Thickness margin = text.Margin;
                margin.Left = 20;
                text.Margin = margin;

                container.Children.Add(smiley);
                container.Children.Add(text);
                container.MouseLeftButtonDown += Rule_MouseLeftButtonDown;

                Rules.Children.Add(container);
            }

        }

        private void Rule_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Goal goal = (Goal)(sender as FrameworkElement).Tag;

            if (goal is GoalEmail)
            {
                GoalSettingManager.Instance.OpenRetrospection(VisType.Week);
            }
            else
            {
                if ((goal as GoalActivity).TimeSpan == RuleTimeSpan.Week || (goal as GoalActivity).TimeSpan == RuleTimeSpan.Month)
                {
                    GoalSettingManager.Instance.OpenRetrospection(VisType.Week);
                }
                else
                {
                    GoalSettingManager.Instance.OpenRetrospection(VisType.Day);
                }
            }
        }
        
        public new bool? ShowDialog()
        {
            Screen mainScreen = Screen.AllScreens[0];

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Size.Height >= mainScreen.WorkingArea.Size.Height && screen.WorkingArea.Width >= mainScreen.WorkingArea.Width)
                {
                    mainScreen = screen;
                }
            }

            this.Left = mainScreen.WorkingArea.Right - this.Width;
            this.Top = mainScreen.WorkingArea.Bottom - this.Height;

            return base.ShowDialog();
        }

        private void Close_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
            GoalSettingManager.Instance.DeleteCachedResults();
        }

        private void Dashboard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
            GoalSettingManager.Instance.OpenRetrospection(VisType.Week);
        }
    }

}