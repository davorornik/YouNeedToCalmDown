using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace YouNeedToCalmDown
{

    public partial class MainWindow : Window
    {
        private Dictionary<string, MMDevice> inputAudioDevices;
        private DispatcherTimer dispatcherTimer;
        private WasapiCapture waveIn;
        private ConfigData config;
        private bool running;
        private MMDevice? dev;
        private Random rnd;
        

        public MainWindow()
        {
            running = false;
            rnd = new Random();
            config = new ConfigData();
            dev = null;
            this.DataContext = config;
            InitializeComponent();
            waveIn = new WasapiCapture();
            inputAudioDevices = GetInputAudioDevices();
            captureList.ItemsSource = new BindingSource(inputAudioDevices, null);
            captureList.DisplayMemberPath = "Key";
            captureList.SelectedIndex = 0;

            dispatcherTimer = new();
            dispatcherTimer.Tick += new EventHandler(Timer_tick);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Start();
        }

        [DllImport("user32")]
        public static extern void LockWorkStation();

        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        private void Timer_tick(object sender, EventArgs e)
        {
            if (dev == null)
                return;
            config.Loudness = dev.AudioMeterInformation.MasterPeakValue;
            if (!running)
                return;
            if (config.Loudness > config.Cutoff)
            {
                config.Count++;
            }
            if (config.TakeAction())
            {
                config.Count = 0;
                DisabledUI();
                // Locks the PC but has a 5% chance to log you out.
                if (rnd.NextDouble() > 0.05)
                {
                    LockWorkStation();
                }
                else
                {
                    ExitWindowsEx(0, 0);
                }
            }
        }


        public class ConfigData : INotifyPropertyChanged
        {
            private double _loudness;
            private double _cutoff;
            private uint _count;
            private uint _minCount;

            public ConfigData()
            {
                _loudness = 0;
                _cutoff = 0.5;
                _count = 0;
                _minCount = 30;
            }

            public double Loudness
            {
                get { return _loudness; }
                set { _loudness = value; OnPropertyChanged("Loudness"); }
            }
            public double Cutoff
            {
                get { return _cutoff; }
                set { _cutoff = value; OnPropertyChanged("Cutoff"); }
            }
            public uint Count
            {
                get { return _count; }
                set { _count = value; OnPropertyChanged("Count"); }
            }

            public uint MinCount
            {
                get { return _minCount; }
                set { _minCount = value; OnPropertyChanged("MinCount"); }
            }


            public bool TakeAction()
            {
                return _count >= _minCount;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private void startstop_Click(object sender, RoutedEventArgs e)
        {
            running = !running;
            if (running)
            {
                EnabledUI();
            }
            else
            {
                DisabledUI();
            }
        }
        private void reset_Click(object sender, RoutedEventArgs e)
        {
            config.Count = 0;
        }

        private void EnabledUI()
        {
            activeStatus.Text = "enabled";
            startstop.Content = "Stop";

        }
        private void DisabledUI()
        {
            activeStatus.Text = "disabled";
            startstop.Content = "Start";
        }


        public static Dictionary<string, MMDevice> GetInputAudioDevices()
        {
            Dictionary<string, MMDevice> retVal = new Dictionary<string, MMDevice>();
            MMDeviceEnumerator enumerator = new();
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
                {
                    if (device.FriendlyName.StartsWith(deviceInfo.ProductName))
                    {
                        retVal.Add(device.FriendlyName, device);
                        break;
                    }
                }
            }

            return retVal;
        }

        private void captureList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (waveIn != null)
                waveIn.StopRecording();
            dev = ((KeyValuePair<string, MMDevice>)captureList.SelectedItem).Value;
            MMDeviceEnumerator devices = new();
            var device = devices.GetDevice(dev.ID);
            waveIn = new WasapiCapture(device);
            waveIn.StartRecording();
        }
    }
}
