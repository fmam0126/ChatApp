using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using ChatApp.Avalonia.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace ChatApp.Avalonia.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AuthService _authService = new();

        private HubConnection? _hubConnection;
        private string? _accessToken;

        // ── Login properties ──
        private string _serverUrl = "https://localhost:7216";
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        private string _username = "";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    OnPropertyChanged(nameof(IsDisconnected));
                    (ConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsDisconnected => !IsConnected;

        private string _statusText = "Disconnected";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        // ── Chat properties ──
        private string _messageText = "";
        public string MessageText
        {
            get => _messageText;
            set
            {
                if (SetProperty(ref _messageText, value))
                {
                    (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<MessageViewModel> Messages { get; } = new();

        // ── Commands ──
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !IsConnecting && !IsConnected);
            DisconnectCommand = new RelayCommand(async () => await DisconnectAsync(), () => IsConnected);
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => IsConnected && !string.IsNullOrWhiteSpace(MessageText));

            LoadConfiguration();
        }

        // ── Configuration ──
        private void LoadConfiguration()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .Build();

                var serverUrl = config.GetSection("Settings")["ServerUrl"];
                if (!string.IsNullOrWhiteSpace(serverUrl))
                {
                    ServerUrl = serverUrl;
                }
            }
            catch
            {
                // Use default ServerUrl if config fails
            }
        }

        // ── Connect ──
        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                StatusText = "Please enter a server URL.";
                return;
            }

            var username = Username.Trim();
            if (username.Length < 3 || username.Length > 30)
            {
                StatusText = "Username must be between 3 and 30 characters.";
                return;
            }

            IsConnecting = true;
            StatusText = "Authenticating...";

            try
            {
                _accessToken = await _authService.LoginAsync(ServerUrl, username);

                if (_accessToken == null)
                {
                    StatusText = $"Username '{username}' is already taken. Choose another.";
                    IsConnecting = false;
                    return;
                }

                StatusText = "Connecting to chat server...";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{ServerUrl}/chatHub", options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_accessToken)!;
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Messages.Add(new MessageViewModel
                        {
                            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                            Username = user,
                            Content = message
                        });
                    });
                });

                _hubConnection.Reconnecting += _ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusText = "Connection lost. Reconnecting...";
                    });
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += _ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusText = "Reconnected!";
                    });
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += ex =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        IsConnected = false;
                        StatusText = ex == null ? "Disconnected." : $"Connection closed: {ex.Message}";
                    });
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();

                IsConnected = true;
                StatusText = $"Connected as {username}";
                MessageText = "";
                (ConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to connect: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
            }
        }

        // ── Disconnect ──
        private async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    StatusText = $"Error disconnecting: {ex.Message}";
                }

                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            _accessToken = null;
            IsConnected = false;
            StatusText = "Disconnected.";
            Messages.Clear();

            (ConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ── Send message ──
        private async Task SendMessageAsync()
        {
            var text = MessageText.Trim();
            if (string.IsNullOrEmpty(text) || _hubConnection == null)
                return;

            MessageText = "";

            try
            {
                await _hubConnection.InvokeAsync("SendMessage", text);
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to send: {ex.Message}";
            }
        }
    }

    // Simple ICommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && _canExecute();

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
