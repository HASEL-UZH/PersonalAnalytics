// Created by André Meyer at University of Zurich
// Created: 2016-04-22
// 
// Licensed under the MIT License.
using Shared.Data;
using System;
using System.Windows.Controls;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for FirstStartWindow.xaml
    /// </summary>
    public partial class FirstStartWindow : UserControl
    {
        
        public FirstStartWindow()
        {
            InitializeComponent();
        }

        internal static void NextClicked()
        {
            Database.GetInstance().SetSettings("FirstStartWindowShown", true);
        }
    }

}