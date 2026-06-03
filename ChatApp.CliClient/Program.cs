using ChatApp.CliClient.Classes;
using ChatApp.CliClient.Interfaces;
using ChatApp.CliClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace ChatApp.CliClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── Load configuration ──
            IConfigurationRoot config;
            try
            {
                config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();
            }
            catch (Exception ex)
            {
                SpectreDisplay.ShowError($"Error loading configuration: {ex.Message}");
                return;
            }

            Settings? settings;
            try
            {
                settings = config.GetSection("Settings").Get<Settings>();
            }
            catch (Exception ex)
            {
                SpectreDisplay.ShowError($"Error loading settings: {ex.Message}");
                return;
            }

            if (settings == null || string.IsNullOrWhiteSpace(settings.ServerUrl))
            {
                SpectreDisplay.ShowError("ServerUrl is missing in appsettings.json.");
                return;
            }

            // ── Welcome ──
            SpectreDisplay.ShowWelcome();

            // ── Authenticate (loop until unique username) ──
            var authService = new AuthService();
            string? accessToken = null;
            string username;

            while (true)
            {
                username = SpectreDisplay.Prompt("Enter your username:").Trim();

                if (username.Length < 3 || username.Length > 30)
                {
                    SpectreDisplay.ShowError("Username must be between 3 and 30 characters.");
                    continue;
                }

                await SpectreDisplay.ShowSpinner("Authenticating...", async () =>
                {
                    accessToken = await authService.LoginAsync(settings.ServerUrl, username);
                });

                if (accessToken != null)
                {
                    SpectreDisplay.ShowSuccess($"Logged in as {username}");
                    break;
                }

                SpectreDisplay.ShowError($"Username '{username}' is already taken. Please choose another.");
            }

            // ── Connect to SignalR hub ──
            var connection = new HubConnectionBuilder()
                .WithUrl($"{settings.ServerUrl}/chatHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                })
                .WithAutomaticReconnect()
                .Build();

            var chatConsole = new ChatConsole();
            chatConsole.Initialize();

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                chatConsole.Enqueue(user, message);
            });

            connection.Reconnecting += _ =>
            {
                SpectreDisplay.ShowInfo("Connection lost. Reconnecting...");
                return Task.CompletedTask;
            };

            connection.Reconnected += _ =>
            {
                SpectreDisplay.ShowSuccess("Reconnected!");
                return Task.CompletedTask;
            };

            try
            {
                await SpectreDisplay.ShowSpinner("Connecting to chat server...", async () =>
                {
                    await connection.StartAsync();
                });
                SpectreDisplay.ShowSuccess("Connected to chat server.");
            }
            catch (Exception ex)
            {
                SpectreDisplay.ShowError($"Failed to connect: {ex.Message}");
                return;
            }

            //// ── Load chat history ──
            //var chatClient = new ChatClient(settings.ServerUrl, accessToken!);
            //try
            //{
            //    List<ChatMessage>? history = null;
            //    await SpectreDisplay.ShowSpinner("Loading chat history...", async () =>
            //    {
            //        var messages = await chatClient.GetMessagesAsync(50);
            //        history = messages.ToList();
            //    });

            //    if (history != null)
            //    {
            //        SpectreDisplay.RenderHistory(history);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    SpectreDisplay.ShowError($"Could not load chat history: {ex.Message}");
            //}

            // ── Chat loop ──
            while (true)
            {
                string? input = await chatConsole.ReadInputAsync();

                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                    break;

                try
                {
                    await connection.InvokeAsync("SendMessage", input);
                }
                catch (Exception ex)
                {
                    chatConsole.Enqueue("System", $"Failed to send: {ex.Message}");
                }
            }

            // ── Disconnect ──
            try
            {
                await connection.StopAsync();
                SpectreDisplay.ShowInfo("Disconnected from chat server.");
            }
            catch (Exception ex)
            {
                SpectreDisplay.ShowError($"Error during disconnect: {ex.Message}");
            }

        }
    }
}
