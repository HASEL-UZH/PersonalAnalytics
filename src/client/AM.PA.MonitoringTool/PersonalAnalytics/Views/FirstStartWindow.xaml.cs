// Created by André Meyer at University of Zurich
// Created: 2016-04-22
// 
// Licensed under the MIT License.
using Shared.Data;
using Shared;
using System.Windows.Controls;

namespace PersonalAnalytics.Views
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl, IFirstStartScreen
    {
        
        public FirstStartWindow()
        {
            InitializeComponent();
        }

        public void NextClicked()
        {
            Database.GetInstance().SetSettings("FirstStartWindowShown", true);
        }

        public string GetTitle()
        {
            return "Personal Analytics: First Start";
        }

        public void PreviousClicked()
        {
            //not needed
        }
    }

}