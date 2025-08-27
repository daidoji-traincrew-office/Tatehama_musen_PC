using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Windows;

namespace Tatehama_musen_PC.ViewModels
{
    public class AudioSettingsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new MMDeviceEnumerator();

        public ObservableCollection<MMDevice> OutputDevices { get; }
        public ObservableCollection<MMDevice> InputDevices { get; }

        private MMDevice? _selectedOutputDevice;
        public MMDevice? SelectedOutputDevice
        {
            get => _selectedOutputDevice;
            set
            {
                if (_selectedOutputDevice == value) return;
                _selectedOutputDevice = value;
                OnPropertyChanged();
                UpdateOutputVolume();
            }
        }

        private MMDevice? _selectedInputDevice;
        public MMDevice? SelectedInputDevice
        {
            get => _selectedInputDevice;
            set
            {
                if (_selectedInputDevice == value) return;
                _selectedInputDevice = value;
                OnPropertyChanged();
                StartMonitoring();
            }
        }

        private double _outputVolume;
        public double OutputVolume
        {
            get => _outputVolume;
            set
            {
                if (Math.Abs(_outputVolume - value) < 0.01) return;
                _outputVolume = value;
                OnPropertyChanged();
                SetOutputVolume(value);
            }
        }

        private double _inputLevel;
        public double InputLevel
        {
            get => _inputLevel;
            private set
            {
                _inputLevel = value;
                OnPropertyChanged();
            }
        }

        public ICommand TestOutputCommand { get; }
        public ICommand CloseCommand { get; }

        private IWavePlayer? _waveOutDevice;
        private WasapiCapture? _captureDevice;

        public event EventHandler? RequestClose;

        public AudioSettingsViewModel()
        {
            OutputDevices = new ObservableCollection<MMDevice>(_enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active));
            InputDevices = new ObservableCollection<MMDevice>(_enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active));

            CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
            TestOutputCommand = new RelayCommand(PlayTestSound, () => _waveOutDevice?.PlaybackState != PlaybackState.Playing);

            InitializeDefaultDevices();
        }

        private void InitializeDefaultDevices()
        {
            try { SelectedOutputDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console); }
            catch { SelectedOutputDevice = OutputDevices.FirstOrDefault(); }

            try { SelectedInputDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console); }
            catch { SelectedInputDevice = InputDevices.FirstOrDefault(); }
        }

        private void UpdateOutputVolume()
        {
            if (SelectedOutputDevice != null)
            {
                OutputVolume = SelectedOutputDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            }
        }

        private void SetOutputVolume(double volume)
        {
            if (SelectedOutputDevice != null)
            {
                SelectedOutputDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volume;
            }
        }

        private void StartMonitoring()
        {
            _captureDevice?.StopRecording();
            _captureDevice?.Dispose();
            _captureDevice = null;

            if (SelectedInputDevice == null) return;

            try
            {
                _captureDevice = new WasapiCapture(SelectedInputDevice);
                _captureDevice.DataAvailable += OnDataAvailable;
                _captureDevice.RecordingStopped += (s, a) => InputLevel = 0;
                _captureDevice.StartRecording();
            }
            catch (Exception)
            {
                // エラー処理: デバイスが使用できない場合など
                InputLevel = 0;
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            var buffer = new WaveBuffer(e.Buffer);
            for (int index = 0; index < e.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];
                if (sample < 0) sample = -sample;
                if (sample > max) max = sample;
            }
            Application.Current.Dispatcher.InvokeAsync(() => InputLevel = max);
        }

        private void PlayTestSound()
        {
            if (SelectedOutputDevice == null) return;

            _waveOutDevice?.Dispose();

            var signalGenerator = new SignalGenerator()
            {
                Gain = 0.2,
                Frequency = 440,
                Type = SignalGeneratorType.Sin
            }.Take(TimeSpan.FromSeconds(1));

            _waveOutDevice = new WasapiOut(SelectedOutputDevice, AudioClientShareMode.Shared, false, 200);
            _waveOutDevice.PlaybackStopped += (s, a) =>
            {
                _waveOutDevice?.Dispose();
                _waveOutDevice = null;
                Application.Current.Dispatcher.InvokeAsync(() => ((RelayCommand)TestOutputCommand).RaiseCanExecuteChanged());
            };

            _waveOutDevice.Init(signalGenerator);
            _waveOutDevice.Play();
            ((RelayCommand)TestOutputCommand).RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            _waveOutDevice?.Stop();
            _waveOutDevice?.Dispose();
            _captureDevice?.StopRecording();
            _captureDevice?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
