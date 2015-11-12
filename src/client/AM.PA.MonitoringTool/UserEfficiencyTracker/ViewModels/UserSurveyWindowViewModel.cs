// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UserEfficiencyTracker.ViewModels
{
    public class UserTask : INotifyPropertyChanged
    {
        public UserTask()
        {
            TaskDescription = string.Empty;
            AutoSuggestionList = new ObservableCollection<string>();
        }

        /// <summary>
        /// updates the current suggestion list based un the users input
        /// </summary>
        /// <param name="typedString"></param>
        /// <param name="previousSurveyEntryTasksWorkedOn"></param>
        public void UpdateAutoSuggestionList(string typedString, List<string> previousSurveyEntryTasksWorkedOn)
        {
            AutoSuggestionList.Clear();

            foreach (var item in previousSurveyEntryTasksWorkedOn)
            {
                if (string.IsNullOrEmpty(typedString)) continue;

                if (item.ToLower().Contains(typedString.ToLower()))
                {
                    AutoSuggestionList.Add(item);
                }
            }

            if (AutoSuggestionList.Count > 0)
            {
                NotifyPropertyChanged("AutoSuggestionList");
            }
        }

        #region PROPERTIES

        private string _taskDescription;
        public string TaskDescription
        {
            get { return _taskDescription; }
            set
            {
                if (value != this._taskDescription)
                {
                    this._taskDescription = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ObservableCollection<String> _autoSuggestionList;
        public ObservableCollection<String> AutoSuggestionList
        {
            get { return _autoSuggestionList; }
            set
            {
                if (value != this._autoSuggestionList)
                {
                    this._autoSuggestionList = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

    public class UserSurveyWindowViewModel
    {
        public ObservableCollection<UserTask> UserTasks { get; set; }

        /// <summary>
        /// initialize list and add empty item
        /// </summary>
        public UserSurveyWindowViewModel()
        {
            UserTasks = new ObservableCollection<UserTask>();
            AddEmpty();
        }

        public void AddEmpty()
        {
            UserTasks.Add(new UserTask());
        }
    }
}
