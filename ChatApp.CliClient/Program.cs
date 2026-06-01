using ChatApp.CliClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using ChatApp.CliClient.Interfaces;
using ChatApp.CliClient.Classes;
using Microsoft.AspNetCore.SignalR.Client;


namespace ChatApp.CliClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return;
            }
            Settings? settings;
            try
            {
                settings = config.GetSection("Settings").Get<Settings>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return;
            }

            //var builder = Host.CreateApplicationBuilder();

            //builder.Services.ConfigureHttpClientDefaults(webBuilder =>
            //{
            //    webBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            //    {
            //        ConnectTimeout = TimeSpan.FromSeconds(10)

            //    });
            //});

            //builder.Services.AddHttpClient<IChatClient, ChatClient>(client =>
            //{
            //    client.BaseAddress = new Uri(settings.ServerUrl);
            //});


            //using var host = builder.Build();
            //var chatClient = host.Services.GetRequiredService<IChatClient>();



            //await chatClient.SendMessageAsync("Hello from CLI client!");
            //var messages = await chatClient.GetMessagesAsync();
            //foreach (var message in messages)
            //{
            //    Console.WriteLine(message.content);
            //}

            var connection = new HubConnectionBuilder()
                .WithUrl($"{settings?.ServerUrl}/chatHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(settings?.AcessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Console.WriteLine($"{user}: {message}");
            });

            try
            {
                Console.WriteLine("Connecting to chat server...");
                await connection.StartAsync();
                Console.WriteLine("Connected to chat server.");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to chat server: {ex.Message}");

            }

            Console.WriteLine("Enter your name: ");
            string userName = Console.ReadLine() ?? "Unknown";

            while (true)
            {
                Console.WriteLine("Enter a message (or 'exit' to quit): ");
                string message = Console.ReadLine() ?? "";
                if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                try
                {
                    await connection.InvokeAsync("SendMessage", userName, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            }

            try
            {
                await connection.StopAsync();
                Console.WriteLine("Disconnected from chat server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from chat server: {ex.Message}");
            }
        }
    }
}
