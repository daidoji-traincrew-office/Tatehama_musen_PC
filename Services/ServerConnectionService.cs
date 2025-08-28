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
        
        // You can add more events here for specific server-to-client messages
        // public event Action<string, object> OnReceiveOffer;

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
                            // This is needed for the self-signed cert in development
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

                    // The Closed event is fired when the connection is lost.
                    _hubConnection.Closed += (error) =>
                    {
                        OnDisconnected?.Invoke();
                        return Task.CompletedTask;
                    };
                    
                    // Register handlers for methods the server will call on the client here
                    // e.g. _hubConnection.On<string, object>("ReceiveOffer", (from, offer) => OnReceiveOffer?.Invoke(from, offer));

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
            if (!IsConnected || _hubConnection == null) return;
            await _hubConnection.StopAsync();
        }

        // You can add more specific InvokeAsync methods here as needed
        // For example:
        // public async Task SendOfferAsync(string target, object offer)
        // {
        //     if (!IsConnected || _hubConnection == null) return;
        //     await _hubConnection.InvokeAsync("SendOffer", target, offer);
        // }
    }
}