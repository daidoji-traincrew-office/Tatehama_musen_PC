using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NAudio.Wave;
using Tatehama_musen_PC.Services;

namespace Tatehama_musen_PC.ViewModels
{
    public enum CallState
    {
        Idle,        // 待機中
        ReadyToCall, // 発信可能 (4桁入力)
        Calling,     // 発信中
        Ringing,     // 着信中
        InCall       // 通話中
    }

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public ServerConnectionService ConnectionService { get; }
        public TenkeyViewModel TenkeyViewModel { get; }

        public string ClientId { get; }

        public ICommand CallCommand { get; }
        public ICommand AnswerCommand { get; }
        public ICommand HangUpCommand { get; }
        public ICommand SimulateIncomingCallCommand { get; }

        private string _connectionStatus = "未接続";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }

        private CallState _currentCallState = CallState.Idle;
        public CallState CurrentCallState
        {
            get => _currentCallState;
            set
            {
                if (_currentCallState == value) return;
                _currentCallState = value;
                OnPropertyChanged();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ((RelayCommand)CallCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)AnswerCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)HangUpCommand).RaiseCanExecuteChanged();
                });
            }
        }

        private string? _remotePhoneNumber;

        // NAudio Sound Players
        private readonly WaveOutEvent _bellPlayer;
        private readonly WaveStream _bellReader;
        private readonly WaveOutEvent _electronicBellPlayer;
        private readonly WaveStream _electronicBellReader;
        private readonly WaveOutEvent _aitenashiPlayer;
        private readonly WaveStream _aitenashiReader1;
        private readonly WaveStream _aitenashiReader2;
        private readonly WaveOutEvent _toriPlayer;
        private readonly WaveStream _toriReader;
        private readonly WaveOutEvent _hangupPlayer;
        private readonly WaveStream _hangupReader;

        private readonly Random _random = new Random();

        public MainViewModel()
        {
            ClientId = Guid.NewGuid().ToString();
            ConnectionService = new ServerConnectionService();
            TenkeyViewModel = new TenkeyViewModel();

            // Initialize NAudio components
            _bellPlayer = new WaveOutEvent();
            _electronicBellPlayer = new WaveOutEvent();
            _aitenashiPlayer = new WaveOutEvent();
            _toriPlayer = new WaveOutEvent();
            _hangupPlayer = new WaveOutEvent();

            // Load all sounds from resources
            _bellReader = GetResourceStream("beru.wav");
            _electronicBellReader = GetResourceStream("denshiberu.wav");
            _aitenashiReader1 = GetResourceStream("aitenashi_1.wav");
            _aitenashiReader2 = GetResourceStream("aitenashi_2.wav");
            _toriReader = GetResourceStream("tori.wav");
            _hangupReader = GetResourceStream("oki.wav");

            // Setup looping for bell sounds
            _bellPlayer.PlaybackStopped += OnBellPlaybackStopped;
            _electronicBellPlayer.PlaybackStopped += OnElectronicBellPlaybackStopped;

            ConnectionService.OnConnected += OnConnectedHandler;
            ConnectionService.OnDisconnected += OnDisconnectedHandler;
            ConnectionService.OnReceiveOffer += OnReceiveOfferHandler;
            ConnectionService.OnTargetNotFound += OnTargetNotFoundHandler;
            ConnectionService.OnCallEnded += OnCallEndedHandler;

            TenkeyViewModel.PropertyChanged += TenkeyViewModel_PropertyChanged;

            CallCommand = new RelayCommand(Call, () => CurrentCallState == CallState.ReadyToCall);
            AnswerCommand = new RelayCommand(Answer, () => CurrentCallState == CallState.Ringing);
            HangUpCommand = new RelayCommand(HangUp, () => CurrentCallState == CallState.Calling || CurrentCallState == CallState.InCall || CurrentCallState == CallState.Ringing);
            SimulateIncomingCallCommand = new RelayCommand<string>(SimulateIncomingCall);
        }

        private WaveStream GetResourceStream(string fileName)
        {
            var uri = new Uri($"pack://application:,,,/sound/{fileName}");
            var streamInfo = Application.GetResourceStream(uri);
            return new WaveFileReader(streamInfo.Stream);
        }

        private void OnConnectedHandler() => Application.Current.Dispatcher.Invoke(() => ConnectionStatus = "接続済み");
        private void OnDisconnectedHandler() => Application.Current.Dispatcher.Invoke(() => ConnectionStatus = "切断されました");

        private void OnTargetNotFoundHandler(string targetPhoneNumber)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StopAllSounds();
                CurrentCallState = CallState.Idle;
                _remotePhoneNumber = null;
                TenkeyViewModel.PhoneNumber = "";

                void ToriPlaybackStopped(object? sender, StoppedEventArgs e)
                {
                    _toriPlayer.PlaybackStopped -= ToriPlaybackStopped;

                    var aitenashiReader = _random.Next(100) < 15 // 確率を85/15に変更
                        ? _aitenashiReader2
                        : _aitenashiReader1;
                    aitenashiReader.Position = 0;
                    _aitenashiPlayer.Init(aitenashiReader);
                    _aitenashiPlayer.Play();
                }

                _toriPlayer.PlaybackStopped += ToriPlaybackStopped;

                _toriReader.Position = 0;
                _toriPlayer.Init(_toriReader);
                _toriPlayer.Play();
            });
        }

        private void OnCallEndedHandler()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StopAllSounds();
                CurrentCallState = CallState.Idle;
                _remotePhoneNumber = null;
                TenkeyViewModel.PhoneNumber = "";
            });
        }

        private void SimulateIncomingCall(string? callerNumber)
        {
            OnReceiveOfferHandler(callerNumber ?? "1001", new { Sdp = "dummy_sdp", Type = "offer" });
        }

        private void OnReceiveOfferHandler(string from, object offer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentCallState != CallState.Idle && CurrentCallState != CallState.ReadyToCall) return;

                _remotePhoneNumber = from;
                CurrentCallState = CallState.Ringing;

                if (int.TryParse(from, out int callerNumber))
                {
                    if (callerNumber >= 1000 && callerNumber < 2000)
                    {
                        _bellReader.Position = 0;
                        _bellPlayer.Init(_bellReader);
                        _bellPlayer.Play();
                    }
                    else if (callerNumber >= 2000 && callerNumber < 3000)
                    {
                        _electronicBellReader.Position = 0;
                        _electronicBellPlayer.Init(_electronicBellReader);
                        _electronicBellPlayer.Play();
                    }
                }
            });
        }

        private async void OnBellPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (CurrentCallState == CallState.Ringing)
            {
                await Task.Delay(1000); // 1秒待機
                _bellReader.Position = 0;
                _bellPlayer.Play();
            }
        }

        private async void OnElectronicBellPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (CurrentCallState == CallState.Ringing)
            {
                await Task.Delay(1000); // 1秒待機
                _electronicBellReader.Position = 0;
                _electronicBellPlayer.Play();
            }
        }

        private void StopAllSounds()
        {
            _bellPlayer.Stop();
            _electronicBellPlayer.Stop();
            _aitenashiPlayer.Stop();
            _toriPlayer.Stop();
            _hangupPlayer.Stop();
        }

        private async void Call()
        {
            var targetPhoneNumber = TenkeyViewModel.PhoneNumber;
            if (string.IsNullOrEmpty(targetPhoneNumber)) return;

            // 自分自身への発信をチェック
            if (targetPhoneNumber == ((App)Application.Current).SelectedPhoneNumber)
            {
                string message;
                int probability = _random.Next(100);

                if (probability < 10) // 10%の確率
                {
                    message = "違うよ？";
                }
                else if (probability < 20) // 10%の確率
                {
                    message = "なんなんだねその番号は";
                }
                else if (probability < 60) // 40%の確率
                {
                    message = "発信先よいか？";
                }
                else // 40%の確率
                {
                    message = "混戦させる気かい？";
                }

                MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _toriReader.Position = 0;
            _toriPlayer.Init(_toriReader);
            _toriPlayer.Play();

            _remotePhoneNumber = targetPhoneNumber;
            CurrentCallState = CallState.Calling;
            
            var dummyOffer = new { Sdp = "dummy_sdp", Type = "offer" };
            await ConnectionService.SendOfferAsync(targetPhoneNumber, dummyOffer);
        }

        private async void Answer()
        {
            if (_remotePhoneNumber == null) return; 

            _toriReader.Position = 0;
            _toriPlayer.Init(_toriReader);
            _toriPlayer.Play();

            StopAllSounds();
            CurrentCallState = CallState.InCall;

            var dummyAnswer = new { Sdp = "dummy_sdp", Type = "answer" };
            await ConnectionService.SendAnswerAsync(_remotePhoneNumber, dummyAnswer);
        }

        private async void HangUp()
        {
            // 終話処理では、まず他の音をすべて止める
            _bellPlayer.Stop();
            _electronicBellPlayer.Stop();
            _aitenashiPlayer.Stop();
            _toriPlayer.Stop();

            // 受話器を置く音を再生
            _hangupReader.Position = 0;
            _hangupPlayer.Init(_hangupReader);
            _hangupPlayer.Play();

            CurrentCallState = CallState.Idle;
            
            await ConnectionService.SendHangUpAsync();

            _remotePhoneNumber = null;
            TenkeyViewModel.PhoneNumber = "";
        }

        private void TenkeyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TenkeyViewModel.PhoneNumber))
            {
                if (CurrentCallState == CallState.Idle || CurrentCallState == CallState.ReadyToCall)
                {
                    CurrentCallState = TenkeyViewModel.PhoneNumber.Length == 4 ? CallState.ReadyToCall : CallState.Idle;
                }
            }
        }

        public async Task ConnectAndRegisterAsync()
        {
            ConnectionStatus = "接続中...";
            var (success, message) = await ConnectionService.ConnectAsync();
            if (success)
            {
                await RegisterWithServer();
            }
            else
            {
                ConnectionStatus = $"接続失敗: {message}";
            }
        }

        private async Task RegisterWithServer()
        {
            try
            {
                var app = (App)Application.Current;
                if (string.IsNullOrEmpty(app.SelectedPhoneNumber))
                {
                    ConnectionStatus = "エラー: 電話番号が選択されていません。";
                    return;
                }
                await ConnectionService.RegisterAsync(ClientId, app.SelectedPhoneNumber);
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"登録失敗: {ex.Message}";
            }
        }

        public async Task ReregisterWithServerAsync()
        {
            if (ConnectionService.IsConnected)
            {
                ConnectionStatus = "再登録中...";
                await RegisterWithServer();
                ConnectionStatus = "接続済み"; // Reset status after re-registering
            }
            else
            {
                ConnectionStatus = "エラー: サーバーに接続されていません。";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            ConnectionService.OnConnected -= OnConnectedHandler;
            ConnectionService.OnDisconnected -= OnDisconnectedHandler;
            ConnectionService.OnReceiveOffer -= OnReceiveOfferHandler;
            ConnectionService.OnTargetNotFound -= OnTargetNotFoundHandler;
            ConnectionService.OnCallEnded -= OnCallEndedHandler;
            TenkeyViewModel.PropertyChanged -= TenkeyViewModel_PropertyChanged;

            _bellPlayer.PlaybackStopped -= OnBellPlaybackStopped;
            _electronicBellPlayer.PlaybackStopped -= OnElectronicBellPlaybackStopped;

            // Dispose NAudio components
            _bellPlayer.Dispose();
            _bellReader.Dispose();
            _electronicBellPlayer.Dispose();
            _electronicBellReader.Dispose();
            _aitenashiPlayer.Dispose();
            _aitenashiReader1.Dispose();
            _aitenashiReader2.Dispose();
            _toriPlayer.Dispose();
            _toriReader.Dispose();
            _hangupPlayer.Dispose();
            _hangupReader.Dispose();

            ConnectionService.DisconnectAsync().Wait();
        }
    }
}