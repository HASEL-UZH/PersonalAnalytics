// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UserEfficiencyTracker.Models;
using UserEfficiencyTracker.ViewModels;

namespace UserEfficiencyTracker
{
    /// <summary>
    /// Interaction logic for UserSurvey.xaml
    /// </summary>
    public partial class UserSurveyWindow : Window
    {
        private readonly NeededSurveyWindowData _neededSurveyWindowData;
        private readonly UserSurveyWindowViewModel _viewModel = new UserSurveyWindowViewModel();

        /// <summary>
        /// Property to get the participant's survey entries
        /// </summary>
        public SurveyEntry CurrentSurveyEntry = new SurveyEntry();

        /// <summary>
        /// Constructor sets previous user survey entry
        /// </summary>
        /// <param name="neededData"></param>
        public UserSurveyWindow(NeededSurveyWindowData neededData)
        {
            InitializeComponent();
            UserTaskList.DataContext = _viewModel;
            _neededSurveyWindowData = neededData;

            // previous survey time if available
            if (_neededSurveyWindowData.PreviousSurveyTimestamp.Year == DateTime.Now.Year)
            {
                PreviousSurveyDateTime.Text = "Please fill out the following survey (last time was: "
                + _neededSurveyWindowData.PreviousSurveyTimestamp.ToShortDateString() + " "
                + _neededSurveyWindowData.PreviousSurveyTimestamp.ToShortTimeString() + "):";
            }


            // set slider value from previous survey
            ProductivitySlider.Value = _neededSurveyWindowData.PreviousSurveyEntryProductivity;
            //SatisfactionSlider.Value = _neededSurveyWindowData.PreviousSurveyEntrySatisfaction;
            EmotionsSlider.Value = _neededSurveyWindowData.PreviousSurveyEntryEmotions;
            //TaskDifficultySlider.Value = _neededSurveyWindowData.PreviousSurveyEntryTaskDifficulty;
            //InterruptibilitySlider.Value = _neededSurveyWindowData.PreviousSurveyEntryInterruptibility;

            // set previously inserted tasks
            if (_neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn == null || _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn.Count == 0)
                PreviousInsertsStackPanel.Visibility = Visibility.Collapsed;
            else
            {
                var numberOfPreviousTasks = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn.Count;
                if (numberOfPreviousTasks >= 1) Prev1TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[0];
                if (numberOfPreviousTasks >= 2) Prev2TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[1];
                if (numberOfPreviousTasks >= 3) Prev3TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[2];
                if (numberOfPreviousTasks >= 4) Prev4TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[3];
                if (numberOfPreviousTasks >= 5) Prev5TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[4];
                if (numberOfPreviousTasks >= 6) Prev6TextBlock.Text = _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn[5];
            }

            // update new survey entry
            CurrentSurveyEntry.TimeStampNotification = neededData.CurrentSurveyEntryNotificationTimeStamp;
        }

        /// <summary>
        /// true if the user has at least inserted some tasks
        /// </summary>
        /// <returns></returns>
        private bool IsSurveyFilledOut()
        {
            var previousProductivitySliderValue = _neededSurveyWindowData.PreviousSurveyEntryProductivity;
            //var previousSatisfactionSliderValue = _neededSurveyWindowData.PreviousSurveyEntrySatisfaction;
            //var previousEmotionsSliderValue = _neededSurveyWindowData.PreviousSurveyEntryEmotions;
            //var previousTaskDifficultySliderValue = _neededSurveyWindowData.PreviousSurveyEntryTaskDifficulty;
            //var PreviousInterruptibilitySliderValue = _neededSurveyWindowData.PreviousSurveyEntryInterruptibility;
            var taskListHasEntries = _viewModel.UserTasks.Any(item => !string.IsNullOrWhiteSpace(item.TaskDescription));

            return (taskListHasEntries
                || ProductivitySlider.Value != previousProductivitySliderValue
                //|| SatisfactionSlider.Value != previousSatisfactionSliderValue
                //|| EmotionsSlider.Value != previousEmotionsSliderValue
                //|| TaskDifficultySlider.Value != previousTaskDifficultySliderValue
                //|| InterruptibilitySlider.Value != PreviousInterruptibilitySliderValue
                //|| SlowNetworkSlider.Value != previousSlowNetworkSliderValue
                );
        }

        /// <summary>
        /// Participant finishes the survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // set values from fields
            CurrentSurveyEntry.TimeStampFinished = DateTime.Now;
            CurrentSurveyEntry.Productivity = (int)ProductivitySlider.Value;
            //CurrentSurveyEntry.Satisfaction = (int)SatisfactionSlider.Value;
            CurrentSurveyEntry.Emotions = (int)EmotionsSlider.Value;
            //CurrentSurveyEntry.TaskDifficulty = (int)TaskDifficultySlider.Value;
            //CurrentSurveyEntry.Interruptibility = (int) InterruptibilitySlider.Value;
            CurrentSurveyEntry.TasksWorkedOn = String.Join(";", _viewModel.UserTasks.Select(t => t.TaskDescription)); // TasksWorkedOn.Text.Replace("\n\r", ";");

            DialogResult = IsSurveyFilledOut(); ;
        }

        private void UserSurveyWindow_OnClosing(object sender, CancelEventArgs e)
        {
            DialogResult = IsSurveyFilledOut(); ;         
        }

        #region Task List User Handling 

        private void PredefinedText_Clicked(object sender, MouseButtonEventArgs e)
        {
            var tb = (sender as TextBlock);
            if (tb == null) return;
            _viewModel.UserTasks.Insert(0, new UserTask { TaskDescription = tb.Text });
        }

        /// <summary>
        /// removes the currently selected task entry from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveTaskEntry_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var userTask = button.DataContext as UserTask;
            if (userTask == null) return;

            _viewModel.UserTasks.Remove(userTask);

            // add empty item if everything was deleted by the user
            if (_viewModel.UserTasks.Count == 0)
            {
                _viewModel.AddEmpty();
            }
        }

        /// <summary>
        /// as soon as the user starts typing, the auto completion box shows results
        /// additionally, we check if a new empty textbox is needed for additional user input
        /// 
        /// also updates the autocompletion suggestion list shown to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            // update the autocompletion suggestion list here
            var txtAuto = sender as TextBox;
            if (txtAuto == null) return;
            var ut = (txtAuto.DataContext as UserTask);
            if (ut == null) return;
            ut.UpdateAutoSuggestionList(txtAuto.Text, _neededSurveyWindowData.PreviousSurveyEntryTasksWorkedOn);

            // add new element if user is filling out the last item
            var val = _viewModel.UserTasks[_viewModel.UserTasks.Count - 1];
            var hasEmptyElement = String.IsNullOrWhiteSpace(val.TaskDescription);
            if (!hasEmptyElement)
            {
                _viewModel.AddEmpty();
            }
        }

        /// <summary>
        /// user selected an item from the suggestion listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbSuggestion_SuggestionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.ItemsSource == null) return;

            var ut = (listBox.DataContext as UserTask);
            if (ut == null || listBox.SelectedItem == null) return;

            ut.TaskDescription = listBox.SelectedItem.ToString();
            ut.AutoSuggestionList.Clear();
        }

        #endregion
    }
}
