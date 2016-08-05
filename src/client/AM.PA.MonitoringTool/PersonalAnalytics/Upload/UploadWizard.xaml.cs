// Created by André Meyer at MSR
// Created: 2016-01-28
// 
// Licensed under the MIT License. 

using Shared.Data;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PersonalAnalytics.Upload
{
    /// <summary>
    /// Interaction logic for UploadWizard.xaml
    /// </summary>
    public partial class UploadWizard : Window
    {
        Uploader _uploader;
        private string _participantId; // also the participants' shared folder name
        private string _localZipFilePath;
        private string _uploadZipFileName;
        private bool _forceClose;

        public object HashTemp { get; private set; }

        public UploadWizard()
        {
            InitializeComponent();
            _uploader = new Uploader();
            StartStep1();
        }

        #region STEP 1 - INFO

        private void StartStep1()
        {
            Step1.Visibility = Visibility.Visible;

            var _quickUploadUserSettings = _uploader.GetQuickUploadData();
            // TODO: verify participant ID beforehand
            if (_quickUploadUserSettings != null)
            {
                // enable quick upload
                QuickUploadEnabled.Visibility = Visibility.Visible;

                // populate user infos
                PrepopulateUserUploadSettings(_quickUploadUserSettings);
            }
        }

        private void PrepopulateUserUploadSettings(string quickUploadUserSettings)
        {
            // TODO: implement (set each textbox + checkbox)
        }

        private void InsertInfosNext_Clicked(object sender, EventArgs e)
        {
            StartStep2();
        }

        private void QuickUploadNext_Clicked(object sender, EventArgs e)
        {
            var res = _uploader.RunQuickUpload();
            res.Wait();
            if (res.Result) StartStep7();
            else CloseWindow();
        }

        #endregion

        #region STEP 2 - USER DETAILS

        private void StartStep2()
        {
            // change 
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Visible;
        }

        private void ParticipateNext_Clicked(object sender, EventArgs e)
        {
            if (!VerifyParticipantId()) return;
            SaveParticipantInfoToFile();
            SaveUploadUserDetailsToDb();
            StartStep3();
        }

        private bool VerifyParticipantId()
        {
            var txt = TbParticipantId.Text.ToUpper();
            if (txt.Length == 3 && (txt.Contains("S") || txt.Contains("F")))
            {
                var hasUploadPermission = _uploader.ValidateParticipantId(TbParticipantId.Text);

                if (hasUploadPermission)
                {
                    _participantId = TbParticipantId.Text.ToUpper();
                    return true;
                }
                else
                {
                    MessageBox.Show("You don't seem to have access permissions for the given participant number (" + TbParticipantId.Text + "). Please make sure you inserted the ID you received via email (e.g. T63) and you are performing the upload from the same account you used to registered for the study.\n\nPlease contact us in case you need assistance.");
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Please insert the subject ID you received with the invitation email (e.g. T63).");
                return false;
            }
        }

        private void SaveParticipantInfoToFile()
        {
            // get info
            var partInfo = "Participant ID: " + _participantId;
            var prodInfo = GetProductGroupString();
            var toolInfo = GetToolInstallationDetails();

            // log info to db
            Database.GetInstance().LogInfo(partInfo);
            Database.GetInstance().LogInfo(prodInfo);
            Database.GetInstance().LogInfo(toolInfo);

            // save file for upload (override)
            using (var w = new StreamWriter(_uploader.GetAdditionalInfoFilePath(), false))
            {
                w.WriteLine(partInfo);
                w.WriteLine("-------------------------------");
                w.WriteLine(prodInfo);
                w.WriteLine("-------------------------------");
                w.WriteLine(toolInfo);
            }
        }

        private void SaveUploadUserDetailsToDb()
        {

        }

        private string GetToolInstallationDetails()
        {
            var str = string.Empty;

            if (CbIsMainMachine.IsChecked.HasValue && CbIsMainMachine.IsChecked.Value == true) str += "This is the MAIN machine. ";
            if (! string.IsNullOrEmpty(TbNumberOfMachines.Text)) str += "Total number of machines where the tool is installed: " + TbNumberOfMachines.Text;

            if (string.IsNullOrEmpty(str)) str = "Only ONE MACHINE.";

            return str;
        }

        private string GetProductGroupString()
        {
            var cb = string.Empty;
            if (Azure.IsChecked.HasValue && Azure.IsChecked.Value == true) cb += "Azure, ";
            if (Dynamics.IsChecked.HasValue && Dynamics.IsChecked.Value == true) cb += "Dynamics, ";
            if (EE.IsChecked.HasValue && EE.IsChecked.Value == true) cb += "EE, ";
            if (Exchange.IsChecked.HasValue && Exchange.IsChecked.Value == true) cb += "Exchange, ";
            if (Office.IsChecked.HasValue && Office.IsChecked.Value == true) cb += "Office, ";
            if (OfficeMac.IsChecked.HasValue && OfficeMac.IsChecked.Value == true) cb += "OfficeMac, ";
            if (OSD.IsChecked.HasValue && OSD.IsChecked.Value == true) cb += "OSD, ";
            if (SQLServer.IsChecked.HasValue && SQLServer.IsChecked.Value == true) cb += "SQLServer, ";
            if (VSO.IsChecked.HasValue && VSO.IsChecked.Value == true) cb += "VSO, ";
            if (Windows.IsChecked.HasValue && Windows.IsChecked.Value == true) cb += "Windows, ";
            if (WindowsPhone.IsChecked.HasValue && WindowsPhone.IsChecked.Value == true) cb += "WindowsPhone, ";
            if (WindowsServices.IsChecked.HasValue && WindowsServices.IsChecked.Value == true) cb += "WindowsServices, ";
            if (Xbox.IsChecked.HasValue && Xbox.IsChecked.Value == true) cb += "Xbox, ";

            if (!string.IsNullOrEmpty(Other.Text)) cb += "\nOther: " + Other.Text + "\n\n";

            if (string.IsNullOrEmpty(cb)) cb = "Nothing selected.";

            return cb;
        }

        #endregion

        #region STEP 3 - SELECT ANONYMIZATION

        private void StartStep3()
        {
            // change 
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Visible;
        }

        private void AnonymizedNext_Clicked(object sender, EventArgs e)
        {
            StartStep4();
        }

        #endregion

        #region STEP 4 - WAIT

        private async void StartStep4()
        {
            // change 
            Step3.Visibility = Visibility.Collapsed;
            Step4.Visibility = Visibility.Visible;

            // anonymize the data
            var obfuscateMeetingTitles = (RBObfuscateMeetingTitles.IsChecked.HasValue) ? RBObfuscateMeetingTitles.IsChecked.Value : false;
            var obfuscateWindowTitles = (RBObfuscateWindowTitles.IsChecked.HasValue) ? RBObfuscateWindowTitles.IsChecked.Value : false;

            var anonymizedDbFilePath = await Task.Run(() => _uploader.AnonymizeCollectedData(obfuscateMeetingTitles, obfuscateWindowTitles));
            if (string.IsNullOrEmpty(anonymizedDbFilePath))
            {
                CloseWindow(); // stop upload wizard if error occurred
                return;
            }

            _localZipFilePath = await Task.Run(() => _uploader.CreateUploadZip(anonymizedDbFilePath));
            if (string.IsNullOrEmpty(_localZipFilePath))
            {
                CloseWindow(); // stop upload wizard if error occurred
                return;
            }

            StartStep5();
        }

        #endregion

        #region STEP 5 - CHECK BEFORE UPLOAD

        private void StartStep5()
        {
            // change 
            Step4.Visibility = Visibility.Collapsed;
            Step5.Visibility = Visibility.Visible;
        }

        private void UploadNow_Clicked(object sender, EventArgs e)
        {
            StartStep6();
        }

        private void SeeZip_Clicked(object sender, EventArgs e)
        {
            Process.Start(_localZipFilePath);
        }

        #endregion

        #region STEP 6 - WAIT FOR UPLOAD

        private async void StartStep6()
        {
            // change 
            Step5.Visibility = Visibility.Collapsed;
            Step6.Visibility = Visibility.Visible;

            // start upload
            _uploadZipFileName = await Task.Run(() => _uploader.UploadZip(_localZipFilePath));
            if (string.IsNullOrEmpty(_uploadZipFileName))
            {
                CloseWindow(); // stop upload wizard if error occurred
                return;
            }

            StartStep7();
        }

        #endregion

        #region STEP 7 - THANKS & SEND EMAIL

        private void StartStep7()
        {
            // change 
            Step6.Visibility = Visibility.Collapsed;
            Step7.Visibility = Visibility.Visible;

            _forceClose = true; // can now close the window without a prompt
        }

        private void CloseUploadWizard_Click(object sender, EventArgs e)
        {
            CloseWindow();
        }

        #endregion

        #region Other

        private void CloseWindow()
        {
            _forceClose = true;
            Database.GetInstance().LogInfo("Upload Wizard closed.");
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_forceClose)
            {
                Database.GetInstance().LogInfo("Upload Wizard closed.");
                base.OnClosing(e);
            }
            else
            {
                var res = MessageBox.Show("Are you sure you want to cancel the Upload Wizard? Sharing your data with us would help us a lot with our research. We can also not send you any personalized insights into your work and productivity, without being able to run some statistical analysis on the collected data set. Please contact us in case you have any questions or troubles concerning the upload. Thank you!\n\nDo you want to cancel the upload?", "Upload Wizard: Cancel the Upload?", MessageBoxButton.YesNo);

                if (res == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Database.GetInstance().LogInfo("Upload Wizard closed.");
                    base.OnClosing(e);
                }
            }
        }

        private void Feedback_Clicked(object sender, EventArgs e)
        {
            Retrospection.Handler.GetInstance().SendFeedback("Feedback Upload", "participant ID: " + _participantId);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
        }

        #endregion
    }
}
