﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NMB.Handlers;
using Victoria;

namespace NMB.Services
{
    public class InitialService
    {
        private DiscordSocketClient _client;
        private ServiceProvider _services;
        private LavaNode _lavaNode;
        private MusicService _musicService;
        private GlobalData _globalData;
        private CommandHandler _commandHandler;

        public InitialService()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _lavaNode = _services.GetRequiredService<LavaNode>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _musicService = _services.GetRequiredService<MusicService>();
            _globalData = _services.GetRequiredService<GlobalData>();

            SubscribeDiscordEvents();
            SubscribeLavaLinkEvents();
        }

        public async Task InitializeAsync()
        {
            await _globalData.InitializeAsync();
            await _client.LoginAsync(TokenType.Bot, GlobalData.Config.DiscordToken);
            await _client.StartAsync();
            await _commandHandler.InitializeAsync();

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
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

                await _client.SetGameAsync(GlobalData.Config.ActivityName, type: GlobalData.Config.ActivityType);
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source, ex.Message);
            }
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
            ActivityType actType = (ActivityType)(DateTime.Now.Ticks % 4);

            await _client.SetGameAsync(GlobalData.Config.ActivityName, type: actType);
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<HttpClient>()
                .AddLavaNode(x =>
                {
                    x.SelfDeaf = false;
                })
                .AddSingleton<InteractiveService>()
                .AddSingleton<GlobalData>()
                .AddSingleton<MusicService>()
                .BuildServiceProvider();
        }
    }
}