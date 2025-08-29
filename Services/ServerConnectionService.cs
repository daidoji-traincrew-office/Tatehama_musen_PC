using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tatehama_musen_PC.Services
{
    public class ServerConnectionService
    {
        private HubConnection? _hubConnection;
        private readonly string[] _serverUrls = {
            "https://localhost:7298/signaling",
            "http://localhost:5232/signaling"
        };

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<string, object>? OnReceiveOffer;
        public event Action<string>? OnTargetNotFound;
        public event Action? OnCallEnded;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public async Task<(bool Success, string Message)> ConnectAsync()
        {
            if (IsConnected) return (true, "Already connected.");

            string lastError = "Failed to connect to any available servers.";

            foreach (var url in _serverUrls)
            {
                try
                {
                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl(url, options =>
                        {
                            options.HttpMessageHandlerFactory = (handler) =>
                            {
                                if (handler is HttpClientHandler clientHandler)
                                {
                                    clientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                                }
                                return handler;
                            };
                        })
                        .WithAutomaticReconnect()
                        .Build();

                    _hubConnection.Closed += (error) =>
                    {
                        OnDisconnected?.Invoke();
                        return Task.CompletedTask;
                    };
                    
                    // Register handlers for server-to-client messages
                    _hubConnection.On<string, object>("ReceiveOffer", (from, offer) => OnReceiveOffer?.Invoke(from, offer));
                    _hubConnection.On<string>("TargetNotFound", (target) => OnTargetNotFound?.Invoke(target));
                    _hubConnection.On("CallEnded", () => OnCallEnded?.Invoke());

                    await _hubConnection.StartAsync();
                    
                    var message = $"Successfully connected to {url}.";
                    OnConnected?.Invoke();
                    return (true, message);
                }
                catch (Exception ex)
                {
                    lastError = $"Failed to connect to {url}: {ex.GetBaseException().Message}";
                    if (_hubConnection != null)
                    {
                        await _hubConnection.DisposeAsync();
                    }
                }
            }
            
            return (false, lastError);
        }

        public async Task RegisterAsync(string clientId, string phoneNumber)
        {
            if (!IsConnected || _hubConnection == null) throw new InvalidOperationException("Not connected to the server.");
            await _hubConnection.InvokeAsync("Register", clientId, phoneNumber);
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection == null) return;
            await _hubConnection.StopAsync();
        }

        public async Task SendOfferAsync(string targetPhoneNumber, object offer)
        {
            if (!IsConnected || _hubConnection == null) return;
            await _hubConnection.InvokeAsync("SendOffer", targetPhoneNumber, offer);
        }

        public async Task SendAnswerAsync(string targetPhoneNumber, object answer)
        {
            if (!IsConnected || _hubConnection == null) return;
            await _hubConnection.InvokeAsync("SendAnswer", targetPhoneNumber, answer);
        }

        public async Task SendIceCandidateAsync(string targetPhoneNumber, object iceCandidate)
        {
            if (!IsConnected || _hubConnection == null) return;
            await _hubConnection.InvokeAsync("SendIceCandidate", targetPhoneNumber, iceCandidate);
        }

        public async Task SendHangUpAsync()
        {
            if (!IsConnected || _hubConnection == null) return;
            await _hubConnection.InvokeAsync("HangUp");
        }
    }
}