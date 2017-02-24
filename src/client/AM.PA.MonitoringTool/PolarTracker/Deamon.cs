// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using PolarTracker.Data;
using Shared;
using System.Collections.Generic;
using PolarTracker.Visualizations;
using System;
using PolarTracker.Views;
using Shared.Data;
using BluetoothLowEnergyConnector;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Concurrent;
using System.Timers;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace PolarTracker
{
    public sealed class Deamon : BaseTracker, ITracker, BluetoothDeviceListener
    {
        private static readonly ConcurrentQueue<HeartRateMeasurement> _hrQueue = new ConcurrentQueue<HeartRateMeasurement>();
        private System.Timers.Timer _saveToDatabaseTimer = new System.Timers.Timer();
        private NotifyIcon _btNotification;
        private bool _showNotification = true;
        private bool _showBluetoothNotification = true;
        private double _previousRR = double.NaN;
        private bool _isConnectedToBluetoothDevice = false;
        private ChooseBluetoothDevice _chooser;
        private bool WasFirstStart = true;

        #region ITracker Stuff

        public Deamon()
        {
            Name = "Polar Tracker";
            if (Settings.IsDetailedCollectionEnabled)
            {
                Name += " (detailed)";
            }
            
            LoggerWrapper.Instance.NewConsoleMessage += OnNewConsoleMessage;
            LoggerWrapper.Instance.NewLogFileMessage += OnNewLogFileMessage;

            _chooser = new ChooseBluetoothDevice();
            _chooser.AddListener(this); // TODO: refactor to register for event
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreatePolarTables();
        }

        public override bool IsFirstStart { get { WasFirstStart = !Database.GetInstance().HasSetting(Settings.TRACKER_ENEABLED_SETTING); return !Database.GetInstance().HasSetting(Settings.TRACKER_ENEABLED_SETTING); } }

        public override string GetStatus()
        {
            return IsRunning ? (Name + " is running. A bluetooth device is " + (_isConnectedToBluetoothDevice ? "connected." : "NOT connected.")) : (Name + " is NOT running.");
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, Settings.IsEnabledByDefault);
        }

        public override async void Start()
        {
            string storedDeviceName = Database.GetInstance().GetSettingsString(Settings.HEARTRATE_TRACKER_ID_SETTING, string.Empty);
            if (storedDeviceName.Equals(string.Empty))
            {
                _chooser.ShowDialog();

                Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
                Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
                Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
                Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
            }
            else
            {
                try
                {
                    var deviceInformation = await Connector.Instance.FindDeviceByName(storedDeviceName);

                    if (deviceInformation == null)
                    {
                        _chooser.ShowDialog();
                    }
                    else
                    {
                        bool connected = false;
                        if (!WasFirstStart)
                        {
                            connected = await Connector.Instance.Connect(deviceInformation);
                        }
                        else
                        {
                            connected = true;
                        }

                        if (connected)
                        {
                            Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
                            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
                            Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
                            Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
                            //FindSensorLocation();
                            StartDatabaseTimer();
                            IsRunning = true;
                        }
                        else
                        {
                            Logger.WriteToConsole("Couldn't establish a connection! Tracker is not running.");
                            IsRunning = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }
            }
            
        }

        public override async void Stop()
        {
            try
            {
                Connector.Instance.ValueChangeCompleted -= OnNewHeartrateMeasurement;
                Connector.Instance.ConnectionLost -= OnConnectionToDeviceLost;
                Connector.Instance.ConnectionReestablished -= OnConnectionReestablished;
                Connector.Instance.BluetoothNotEnabled -= OnBluetoothNotEnabled;

                Connector.Instance.Stop();
                _saveToDatabaseTimer.Stop();
                await Task.Run(() =>
                    SaveToDatabase()
                );
                IsRunning = false;
                _isConnectedToBluetoothDevice = false;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        void BluetoothDeviceListener.OnTrackerDisabled() 
        {
            _chooser.Close();
            IsRunning = false;
            _isConnectedToBluetoothDevice = false;
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new PolarVisualizationForDay(date) };
        }
        
        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization> { new PolarVisualizationForWeek(date) };
        }

        public override List<FirstStartScreenContainer> GetStartScreens()
        {
            var window = new FirstStartWindow();
            return new List<FirstStartScreenContainer>() { new FirstStartScreenContainer(window, Name, window.NextClicked) };
        }

        #endregion

        #region Events and Helper Methods

        public void ChangeEnableState(bool? polarTrackerEnabled)
        {
            Logger.WriteToConsole(Name + " is now " + (polarTrackerEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, polarTrackerEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to " + polarTrackerEnabled.Value);

            if (polarTrackerEnabled.Value && IsRunning)
            {
                CreateDatabaseTablesIfNotExist();
                Start();
            }
            else if (!polarTrackerEnabled.Value && IsRunning)
            {
                Stop();
            }
            else
            {
                Logger.WriteToConsole("Don't do anything, tracker is paused");
            }
        }

        private void OnBluetoothNotEnabled()
        {
            Logger.WriteToConsole("Bluetooth not enabled!");
            if (_showBluetoothNotification)
            {
                _btNotification = new NotifyIcon();
                _btNotification.Visible = true;
                _btNotification.BalloonTipTitle = "PersonalAnalytics: Bluetooth not enabled!";
                _btNotification.BalloonTipText = Name + ": Bluetooth is not enabled. To use the biometrics tracker, please enable bluetooth.";
                _btNotification.Icon = SystemIcons.Exclamation;
                _btNotification.Text = Name + ": Bluetooth not enabled!";
                _btNotification.ShowBalloonTip(60 * 1000);

            }
            _showBluetoothNotification = false;
        }

        void BluetoothDeviceListener.OnConnectionEstablished(string deviceID)
        {
            _chooser.Close();
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, deviceID);
            //Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
            //Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            //Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
            //Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
            //FindSensorLocation();
            StartDatabaseTimer();
            IsRunning = true;
        }

        private void OnConnectionReestablished()
        {
            Logger.WriteToConsole("Connection restablished!");
            if (_btNotification != null)
            {
                _btNotification.Dispose();
            }

            _isConnectedToBluetoothDevice = true;
        }

        private void StartDatabaseTimer()
        {

            if (_saveToDatabaseTimer != null && !_saveToDatabaseTimer.Enabled)
            {
                _saveToDatabaseTimer.Interval = Settings.SAVE_TO_DATABASE_INTERVAL;
                _saveToDatabaseTimer.Elapsed += OnSaveToDatabase;
                _saveToDatabaseTimer.Start();
            }
        }

        private async void OnSaveToDatabase(object sender, ElapsedEventArgs e)
        {
            await Task.Run(() => SaveToDatabase());
        }

        private void SaveToDatabase()
        {
            try
            {
                if (_hrQueue.Count > 0)
                {
                    _isConnectedToBluetoothDevice = true;

                    var measurements = new List<HeartRateMeasurement>();

                    HeartRateMeasurement measurement = null;
                    while (!_hrQueue.IsEmpty)
                    {
                        _hrQueue.TryDequeue(out measurement);
                        if (measurement != null)
                        {
                            if (!double.IsNaN(_previousRR))
                            {
                                measurement.RRDifference = Math.Abs(measurement.RRInterval - _previousRR);
                            }
                            _previousRR = measurement.RRInterval;
                            measurements.Add(measurement);
                        }
                    }

                    if (Settings.IsDetailedCollectionEnabled)
                    {
                        DatabaseConnector.AddHeartMeasurementsToDatabase(measurements, false);
                    }

                    var ts = (measurements.Count >= 0) ? measurements[0].Timestamp : string.Empty;
                    var heartRateValues = measurements.Where(x => !double.IsNaN(x.HeartRateValue)).Select(x => x.HeartRateValue);
                    var rrdifferences = measurements.Where(x => !double.IsNaN(x.RRDifference)).Select(x => x.RRDifference);
                    var rrintervals = measurements.Where(x => !double.IsNaN(x.RRInterval)).Select(x => x.RRInterval);

                    var averages = new HeartRateMeasurement()
                    {
                        HeartRateValue = (heartRateValues.Count() == 0) ? double.NaN : heartRateValues.Average(),
                        RRDifference = (rrdifferences.Count() == 0) ? double.NaN : rrdifferences.Average(),
                        RRInterval = (rrintervals.Count() == 0) ? double.NaN : rrintervals.Average(),
                        Timestamp = ts
                    };
                    DatabaseConnector.AddHeartMeasurementsToDatabase(new List<HeartRateMeasurement>() { averages }, true);
                }
                else
                {
                    _isConnectedToBluetoothDevice = false;
                    Logger.WriteToConsole("Nothing to save...");
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        //private void FindSensorLocation()
        //{
        //    string sensorLocation = Connector.Instance.GetBodySensorLocation().Result.ToString();
        //    if (sensorLocation != null)
        //    {
        //        isConnectedToBluetoothDevice = true;
        //        Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_LOCATION_SETTING, sensorLocation);
        //        Logger.WriteToConsole("Body sensor location: " + sensorLocation);
        //    }
        //    else
        //    {
        //        Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_LOCATION_SETTING, Settings.HEARTRATE_TRACKER_LOCATION_UNKNOWN);
        //        Logger.WriteToConsole("Body sensor location unknown");
        //    }
        //}

        private void OnConnectionToDeviceLost(string deviceName)
        {
            if (_showNotification)
            {
                _btNotification = new NotifyIcon();
                _btNotification.Visible = true;
                _btNotification.BalloonTipTitle = "PersonalAnalytics: Connection lost!";
                _btNotification.BalloonTipText = Name + " has lost the connection to: " + deviceName;
                _btNotification.Icon = SystemIcons.Exclamation;
                _btNotification.Text = Name + ": Connection to bluetooth device lost!";
                _btNotification.ShowBalloonTip(60 * 1000);
            }
            _showNotification = false;
            _isConnectedToBluetoothDevice = false;
        }

        private async void OnNewHeartrateMeasurement(List<HeartRateMeasurement> heartRateMeasurementValue)
        {
            foreach (HeartRateMeasurement measurement in heartRateMeasurementValue)
            {
                await Task.Run(() => _hrQueue.Enqueue(measurement));
            }
        }

        private void OnNewLogFileMessage(Exception error)
        {
            Logger.WriteToLogFile(error);
        }

        private void OnNewConsoleMessage(string message)
        {
            Logger.WriteToConsole(message);
        }

        #endregion
    }
}