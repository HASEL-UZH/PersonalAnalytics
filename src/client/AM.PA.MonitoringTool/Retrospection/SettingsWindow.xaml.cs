// Created by André Meyer at MSR
// Created: 2016-01-03
// 
// Licensed under the MIT License. 

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Globalization;
using System.Windows.Controls;

namespace Retrospection
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private bool defaultPopUpIsEnabled;
        private int defaultPopUpInterval;
        private bool defaultOffice365ApiEnabled;
        private bool defaultUserInputTrackerEnabled;
        private bool defaultOpenRetrospectionInFullScreen;
        private bool defaultTimeSpentShowProgramsEnabled;
        private bool defaultTimeSpentShowEmailsEnabled;
        private bool defaultPolarTrackerEnabled;
        private bool defaultFitbitTrackerEnabled;

        private string minutesStr = " minutes";
        private List<ITracker> _trackers;

        public SettingsDto UpdatedSettingsDto;

        public SettingsWindow(List<ITracker> trackers, SettingsDto dto, string version)
        {
            InitializeComponent();
            _trackers = trackers;
            TbVersion.Text = "Version: " + version;
            SetDefaultValues(dto);
        }

        private void SetDefaultValues(SettingsDto dto)
        {
            // get defaults
            defaultPopUpIsEnabled = dto.PopUpEnabled.Value;
            defaultPopUpInterval = dto.PopUpInterval.Value;
            defaultOffice365ApiEnabled = dto.Office365ApiEnabled.Value;
            defaultUserInputTrackerEnabled = dto.UserInputTrackerEnabled.Value;
            defaultOpenRetrospectionInFullScreen = dto.OpenRetrospectionInFullScreen.Value;
            defaultTimeSpentShowProgramsEnabled = dto.TimeSpentShowProgramsEnabled.Value;
            defaultTimeSpentShowEmailsEnabled = dto.TimeSpentShowEmailsEnabled.Value;
            defaultPolarTrackerEnabled = dto.PolarTrackerEnabled.Value;
            defaultFitbitTrackerEnabled = dto.FitbitTrackerEnabled.Value;

            // no changes yet, disable buttons by default
            SaveButtonsEnabled(false);

            // set previous values & add event handlers
            CbPopUpsEnabled.IsChecked = defaultPopUpIsEnabled;
            CbPopUpsEnabled.Checked += CbPopUpsEnabled_Checked;
            CbPopUpsEnabled.Unchecked += CbPopUpsEnabled_Checked;

            CbPopUpInterval.SelectedValue = defaultPopUpInterval + minutesStr;

            CbOfficeApiEnabled.IsChecked = defaultOffice365ApiEnabled;
            CbOfficeApiEnabled.Checked += CbChecked_Update;
            CbOfficeApiEnabled.Unchecked += CbChecked_Update;

            CbOpenRetrospectionInFullScreen.IsChecked = defaultOpenRetrospectionInFullScreen;
            CbOpenRetrospectionInFullScreen.Checked += CbChecked_Update;
            CbOpenRetrospectionInFullScreen.Unchecked += CbChecked_Update;

            CbTimeSpentShowProgramsEnabled.IsChecked = defaultTimeSpentShowProgramsEnabled;
            CbTimeSpentShowProgramsEnabled.Checked += CbChecked_Update;
            CbTimeSpentShowProgramsEnabled.Unchecked += CbChecked_Update;

            CbTimeSpentShowEmailsEnabled.IsChecked = defaultTimeSpentShowEmailsEnabled;
            CbTimeSpentShowEmailsEnabled.Checked += CbChecked_Update;
            CbTimeSpentShowEmailsEnabled.Unchecked += CbChecked_Update;

            CbUserInputTrackerEnabled.IsChecked = defaultUserInputTrackerEnabled;
            CbUserInputTrackerEnabled.Checked += CbChecked_Update;
            CbUserInputTrackerEnabled.Unchecked += CbChecked_Update;

            if (CbPopUpsEnabled.IsChecked.Value)
            {
                CbPopUpInterval.IsEnabled = true;
            }
            CbPopUpInterval.SelectionChanged += CbPopUpInterval_SelectionChanged;

            PolarEnabled.IsChecked = defaultPolarTrackerEnabled;
            PolarEnabled.Checked += CbChecked_Update;
            PolarEnabled.Unchecked += CbChecked_Update;

            FitbitEnabled.IsChecked = defaultFitbitTrackerEnabled;
            FitbitEnabled.Checked += CbChecked_Update;
            FitbitEnabled.Unchecked += CbChecked_Update;
        }
        
        #region User Changed Values

        private void CbPopUpsEnabled_Checked(object sender, RoutedEventArgs e)
        {
            CbPopUpInterval.IsEnabled = CbPopUpsEnabled.IsChecked.Value;
            UpdateSettingsChanged();
        }

        private void CbChecked_Update(object sender, RoutedEventArgs e)
        {
            UpdateSettingsChanged();
        }

        private void CbPopUpInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsChanged();
        }

        private void UpdateSettingsChanged()
        {
            try
            {
                if ((defaultPopUpIsEnabled != CbPopUpsEnabled.IsChecked.Value) ||
                 (defaultPopUpInterval + minutesStr != CbPopUpInterval.SelectedValue.ToString()) ||
                 (defaultOffice365ApiEnabled != CbOfficeApiEnabled.IsChecked.Value) ||
                 (defaultUserInputTrackerEnabled != CbUserInputTrackerEnabled.IsChecked.Value) ||
                 (defaultOpenRetrospectionInFullScreen != CbOpenRetrospectionInFullScreen.IsChecked.Value) ||
                 (defaultTimeSpentShowEmailsEnabled != CbTimeSpentShowEmailsEnabled.IsChecked.Value) ||
                 (defaultTimeSpentShowProgramsEnabled != CbTimeSpentShowProgramsEnabled.IsChecked.Value) ||
                 (defaultPolarTrackerEnabled != PolarEnabled.IsChecked.Value) ||
                 (defaultFitbitTrackerEnabled != FitbitEnabled.IsChecked.Value)
                 )
                {
                    SaveButtonsEnabled(true);
                }
                else
                {
                    SaveButtonsEnabled(false);
                }
            }
            catch
            {
                SaveButtonsEnabled(false);
            }
        }

        private void SaveButtonsEnabled(bool isEnabled)
        {
            BtnSave.IsEnabled = isEnabled;
            //BtnCancel.IsEnabled = isEnabled;
        }
        
#endregion


        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            var dto = new SettingsDto();

            try
            {
                if ((defaultPopUpIsEnabled != CbPopUpsEnabled.IsChecked.Value))
                {
                    dto.PopUpEnabled = CbPopUpsEnabled.IsChecked.Value;
                }
                else { dto.PopUpEnabled = null;  }

                if (defaultPopUpInterval + minutesStr != CbPopUpInterval.SelectedValue.ToString())
                {
                    var intervalString = CbPopUpInterval.SelectedValue.ToString().Replace(minutesStr, "");
                    dto.PopUpInterval = int.Parse(intervalString, CultureInfo.InvariantCulture);
                }
                else { dto.PopUpInterval = null; }

                if (defaultOffice365ApiEnabled != CbOfficeApiEnabled.IsChecked.Value)
                {
                    dto.Office365ApiEnabled = CbOfficeApiEnabled.IsChecked.Value;
                }
                else { dto.Office365ApiEnabled = null; }

                if (defaultOpenRetrospectionInFullScreen != CbOpenRetrospectionInFullScreen.IsChecked.Value)
                {
                    dto.OpenRetrospectionInFullScreen = CbOpenRetrospectionInFullScreen.IsChecked.Value;
                }
                else { dto.OpenRetrospectionInFullScreen = null; }

                if (defaultTimeSpentShowEmailsEnabled != CbTimeSpentShowEmailsEnabled.IsChecked.Value)
                {
                    dto.TimeSpentShowEmailsEnabled = CbTimeSpentShowEmailsEnabled.IsChecked.Value;
                }
                else { dto.TimeSpentShowEmailsEnabled = null; }

                if (defaultTimeSpentShowProgramsEnabled != CbTimeSpentShowProgramsEnabled.IsChecked.Value)
                {
                    dto.TimeSpentShowProgramsEnabled = CbTimeSpentShowProgramsEnabled.IsChecked.Value;
                }
                else { dto.TimeSpentShowProgramsEnabled = null; }

                if (defaultUserInputTrackerEnabled != CbUserInputTrackerEnabled.IsChecked.Value)
                {
                    dto.UserInputTrackerEnabled = CbUserInputTrackerEnabled.IsChecked.Value;
                }
                else { dto.UserInputTrackerEnabled = null; }

                if (defaultPolarTrackerEnabled != PolarEnabled.IsChecked.Value)
                {
                    dto.PolarTrackerEnabled = PolarEnabled.IsChecked.Value;
                }
                else { dto.PolarTrackerEnabled = null; }

                if (defaultFitbitTrackerEnabled != FitbitEnabled.IsChecked.Value)
                {
                    dto.FitbitTrackerEnabled = FitbitEnabled.IsChecked.Value;
                }
                else { dto.FitbitTrackerEnabled = null; }
            }
            catch { }

            UpdatedSettingsDto = dto;
            DialogResult = true;
            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Feedback_Clicked(object sender, EventArgs e)
        {
            Handler.GetInstance().SendFeedback();
        }
    }
}
