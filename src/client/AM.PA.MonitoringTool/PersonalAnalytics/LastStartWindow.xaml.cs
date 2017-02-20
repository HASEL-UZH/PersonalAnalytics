using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for LastStartWindow.xaml
    /// </summary>
    public partial class LastStartWindow : UserControl
    {
        public LastStartWindow(List<string> trackerNames)
        {
            InitializeComponent();

            foreach (string tracker in trackerNames)
            {
                Trackers.Inlines.Add(tracker + "\n");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
