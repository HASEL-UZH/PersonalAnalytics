// Created by André Meyer at MSR
// Created: 2016-01-03
// 
// Licensed under the MIT License. 

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;

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
        private bool defaultFitbitTokenRemoveEnabled;
        private bool defaultFitbitTokenRevoked;
        private bool defaultFlowLightEnabled;
        private bool defaultSkypeForBusinessEnabled;
        private bool defaultFlowLightAutomaticEnabled;
        private bool defaultFlowLightDnDEnabled;
        private int defaultFlowLightSensitivityLevel;
        private string[] defaultFlowLightBlacklist;

        private string minutesStr = " minutes";
        private List<ITracker> _trackers;

        public SettingsDto UpdatedSettingsDto;

        public SettingsWindow(List<ITracker> trackers, SettingsDto dto, string version)
        {
            InitializeComponent();
            _trackers = trackers;
            TbVersion.Text = "Version: " + version;
            SetDefaultValues(dto);

            // show/hide FlowLight tab depending on availability
            FlowLightSettingsTab.Visibility = (dto.FlowLightAvailable.HasValue && dto.FlowLightAvailable.Value) ? Visibility.Visible : Visibility.Collapsed;
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
            defaultFlowLightEnabled = dto.FlowLightEnabled.Value;
            defaultSkypeForBusinessEnabled = dto.FlowLightSkypeForBusinessEnabled.Value;
            defaultFlowLightAutomaticEnabled = dto.FlowLightAutomaticEnabled.Value;
            defaultFlowLightDnDEnabled = dto.FlowLightDnDEnabled.Value;
            defaultFlowLightSensitivityLevel = dto.FlowLightSensitivityLevel.Value;
            defaultFlowLightBlacklist = dto.FlowLightBlacklist;
            defaultFitbitTokenRemoveEnabled = dto.FitbitTokenRevokEnabled.Value;
            defaultFitbitTokenRevoked = dto.FitbitTokenRevoked.Value;
            
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

            FitbitRevoke.IsEnabled = defaultFitbitTokenRemoveEnabled;

            CbFlowLightEnabled.IsChecked = defaultFlowLightEnabled;
            CbFlowLightEnabled.Checked += CbChecked_Update;
            CbFlowLightEnabled.Unchecked += CbChecked_Update;

            CbFlowLightSkypeForBusinessEnabled.IsChecked = defaultSkypeForBusinessEnabled;
            CbFlowLightSkypeForBusinessEnabled.Checked += CbChecked_Update;
            CbFlowLightSkypeForBusinessEnabled.Unchecked += CbChecked_Update;

            RbFlowLightAutomatic.IsChecked = defaultFlowLightAutomaticEnabled;
            RbFlowLightManual.IsChecked = !defaultFlowLightAutomaticEnabled;
            RbFlowLightAutomatic.Checked += CbChecked_Update;
            RbFlowLightAutomatic.Unchecked += CbChecked_Update;

            CbFlowLightAllowDnD.IsChecked = defaultFlowLightDnDEnabled;
            CbFlowLightAllowDnD.Checked += CbChecked_Update;
            CbFlowLightAllowDnD.Unchecked += CbChecked_Update;

            SrFlowLightSensitivity.Value = defaultFlowLightSensitivityLevel;
            SrFlowLightSensitivity.ValueChanged += CbChecked_Update;

            foreach (string runningApplication in GetRunningApps())
            {
                LbFlowLightRunningApps.Items.Add(runningApplication);
            }
            foreach (string blacklistedApplication in defaultFlowLightBlacklist)
            {
                LbFlowLightBlacklistedApps.Items.Add(blacklistedApplication);
            }

            BtFlowLightMoveToBlacklist.Click += BtFlowLightMoveToBlacklist_Click;
            BtFlowLightMoveFromBlacklist.Click += BtFlowLightMoveFromBlacklist_Click;
        }

        private void BtFlowLightMoveFromBlacklist_Click(object sender, RoutedEventArgs e)
        {
            string selectedItem = LbFlowLightBlacklistedApps.SelectedValue.ToString();
            int selectedIndex = LbFlowLightBlacklistedApps.SelectedIndex;
            LbFlowLightBlacklistedApps.Items.RemoveAt(selectedIndex);
            LbFlowLightRunningApps.Items.Add(selectedItem);

            UpdateSettingsChanged();
        }

        private void BtFlowLightMoveToBlacklist_Click(object sender, RoutedEventArgs e)
        {
            string selectedItem = LbFlowLightRunningApps.SelectedValue.ToString();
            int selectedIndex = LbFlowLightRunningApps.SelectedIndex;
            LbFlowLightRunningApps.Items.RemoveAt(selectedIndex);
            LbFlowLightBlacklistedApps.Items.Add(selectedItem);

            UpdateSettingsChanged();
        }

        private List<string> GetRunningApps()
        {
            var ret = new List<string>();

            foreach (var proc in Process.GetProcesses())
            {
                var handle = IntPtr.Zero;
                try
                {
                    handle = proc.MainWindowHandle;
                }
                catch (Exception) { }

                if (handle != IntPtr.Zero && proc.ProcessName != "explorer")
                {
                    ret.Add(proc.ProcessName);
                }
            }

            return ret;
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
                string[] blacklist = new string[LbFlowLightBlacklistedApps.Items.Count];
                LbFlowLightBlacklistedApps.Items.CopyTo(blacklist, 0);

                if ((defaultPopUpIsEnabled != CbPopUpsEnabled.IsChecked.Value) ||
                 (defaultPopUpInterval + minutesStr != CbPopUpInterval.SelectedValue.ToString()) ||
                 (defaultOffice365ApiEnabled != CbOfficeApiEnabled.IsChecked.Value) ||
                 (defaultUserInputTrackerEnabled != CbUserInputTrackerEnabled.IsChecked.Value) ||
                 (defaultOpenRetrospectionInFullScreen != CbOpenRetrospectionInFullScreen.IsChecked.Value) ||
                 (defaultTimeSpentShowEmailsEnabled != CbTimeSpentShowEmailsEnabled.IsChecked.Value) ||
                 (defaultTimeSpentShowProgramsEnabled != CbTimeSpentShowProgramsEnabled.IsChecked.Value) ||
                 (defaultPolarTrackerEnabled != PolarEnabled.IsChecked.Value) ||
                 (defaultFitbitTrackerEnabled != FitbitEnabled.IsChecked.Value) ||
                 (defaultFlowLightEnabled != CbFlowLightEnabled.IsChecked.Value) ||
                 (defaultSkypeForBusinessEnabled != CbFlowLightSkypeForBusinessEnabled.IsChecked.Value) ||
                 (defaultFlowLightAutomaticEnabled != RbFlowLightAutomatic.IsChecked.Value) ||
                 (defaultFlowLightDnDEnabled != CbFlowLightAllowDnD.IsChecked.Value) ||
                 (defaultFlowLightSensitivityLevel != SrFlowLightSensitivity.Value) ||
                 (!defaultFlowLightBlacklist.SequenceEqual(blacklist))
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
                else { dto.PopUpEnabled = null; }

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

                if (defaultFlowLightEnabled != CbFlowLightEnabled.IsChecked.Value)
                {
                    dto.FlowLightEnabled = CbFlowLightEnabled.IsChecked.Value;
                }
                else { dto.FlowLightEnabled = null; }

                if (defaultSkypeForBusinessEnabled != CbFlowLightSkypeForBusinessEnabled.IsChecked.Value)
                {
                    dto.FlowLightSkypeForBusinessEnabled = CbFlowLightSkypeForBusinessEnabled.IsChecked.Value;
                }
                else { dto.FlowLightSkypeForBusinessEnabled = null; }

                if (defaultFlowLightAutomaticEnabled != RbFlowLightAutomatic.IsChecked.Value)
                {
                    dto.FlowLightAutomaticEnabled = RbFlowLightAutomatic.IsChecked;
                }
                else { dto.FlowLightAutomaticEnabled = null; }

                if (defaultFlowLightDnDEnabled != CbFlowLightAllowDnD.IsChecked.Value)
                {
                    dto.FlowLightDnDEnabled = CbFlowLightAllowDnD.IsChecked;
                }
                else { dto.FlowLightDnDEnabled = null; }

                if (defaultFlowLightSensitivityLevel != SrFlowLightSensitivity.Value)
                {
                    dto.FlowLightSensitivityLevel = (int)SrFlowLightSensitivity.Value;
                }
                else { dto.FlowLightSensitivityLevel = null; }

                string[] blacklist = new string[LbFlowLightBlacklistedApps.Items.Count];
                LbFlowLightBlacklistedApps.Items.CopyTo(blacklist, 0);
                if (!defaultFlowLightBlacklist.SequenceEqual(blacklist))
                {
                    dto.FlowLightBlacklist = blacklist;
                }
                else { dto.FlowLightBlacklist = null; }
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

        private void FitbitRevoke_Click(object sender, RoutedEventArgs e)
        {
            FitbitRevoke.IsEnabled = false;

            //  FitbitConnector.RevokeAccessToken(SecretStorage.GetAccessToken());
            UpdatedSettingsDto = new SettingsDto();
            UpdatedSettingsDto.FitbitTokenRevoked = true;

            DialogResult = true;
            this.Close();
        }
    }
}
