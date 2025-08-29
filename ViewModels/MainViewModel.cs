
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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

        private string _connectionStatus = "未接続";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        private CallState _currentCallState = CallState.Idle;
        public CallState CurrentCallState
        {
            get => _currentCallState;
            set
            {
                _currentCallState = value;
                OnPropertyChanged();
            }
        }

        private bool _isOutgoingCall;
        public bool IsOutgoingCall
        {
            get => _isOutgoingCall;
            set
            {
                if (_isOutgoingCall != value)
                {
                    _isOutgoingCall = value;
                    OnPropertyChanged();
                }
            }
        }


        public MainViewModel()
        {
            ClientId = Guid.NewGuid().ToString();
            ConnectionService = new ServerConnectionService();
            TenkeyViewModel = new TenkeyViewModel();

            ConnectionService.OnConnected += () => Application.Current.Dispatcher.Invoke(() => ConnectionStatus = "接続済み");
            ConnectionService.OnDisconnected += () => Application.Current.Dispatcher.Invoke(() => ConnectionStatus = "切断されました");

            TenkeyViewModel.PropertyChanged += TenkeyViewModel_PropertyChanged;
        }

        private void TenkeyViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TenkeyViewModel.PhoneNumber))
            {
                // Only change state if currently idle or ready to call
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
                // You could add a status update here, e.g., ConnectionStatus = "登録済み";
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
            TenkeyViewModel.PropertyChanged -= TenkeyViewModel_PropertyChanged;
            (TenkeyViewModel as IDisposable)?.Dispose();
        }
    }
}
