using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NMB.Services;
using Victoria;

namespace NMB
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private LavaNode _lavaNode;
        private MusicService _musicService;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                _client = services.GetRequiredService<DiscordSocketClient>();
                _lavaNode = services.GetRequiredService<LavaNode>();
                _musicService = services.GetRequiredService<MusicService>();

                var token = "ODg3MDIxNTMyNzQ3MDA2MDMy.YT-FLQ.eyTHMx4kt_aAVaH_TdFNgIG9CAg";

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                SubscribeDiscordEvents();
                SubscribeLavaLinkEvents();

                // Block this task until the program is closed.
                await Task.Delay(Timeout.Infinite);
            }
        }

        private void SubscribeDiscordEvents()
        {
            _client.Ready += ReadyAsync;
            _client.Log += LogAsync;
        }

        private void SubscribeLavaLinkEvents()
        {
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnTrackEnded += _musicService.TrackEnded;
        }

        private async Task ReadyAsync()
        {
            try
            {
                if (!_lavaNode.IsConnected)
                    await _lavaNode.ConnectAsync();
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source, ex.Message);
            }
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddLavaNode(x =>
                {
                    x.SelfDeaf = false;
                })
                .AddSingleton<InteractiveService>()
                .AddSingleton<MusicService>()
                .BuildServiceProvider();
        }
    }
}