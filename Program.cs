using System.IO;
using System.Net.Http;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NMB.Handlers;
using NMB.Services;
using Serilog;
using Serilog.Events;
using Victoria;

namespace NMB
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CreateGlobalLoggerConfiguration();
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {

            var builder = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration(x =>
                    {
                        var conf = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("config.json", false, true)
                        .Build();

                        x.AddConfiguration(conf);
                    })
                    .UseSerilog()
                    .ConfigureDiscordHost((context, config) =>
                    {
                        config.SocketConfig = new DiscordSocketConfig
                        {
                            LogLevel = LogSeverity.Verbose,
                            AlwaysDownloadUsers = true,
                            MessageCacheSize = 200,
                            DefaultRetryMode = RetryMode.AlwaysRetry,

                        };

                        config.Token = context.Configuration["DiscordToken"];
                        config.LogFormat = (message, exception) => $"{message.Source}: {message.Message}";
                    })
                    .UseCommandService((context, config) =>
                    {
                        config = new CommandServiceConfig()
                        {
                            CaseSensitiveCommands = false,
                            LogLevel = LogSeverity.Verbose,
                        };
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services
                        .AddHostedService<CommandHandler>()
                        .AddSingleton<HttpClient>()
                        .AddSingleton<InteractiveService>()
                        .AddSingleton<MusicService>()
                        .AddLavaNode(x =>
                        {
                            x.SelfDeaf = true;
                            x.EnableResume = true;
                            x.ReconnectDelay = new System.TimeSpan(0, 0, 2);
                        })
                        ;
                    });
            return builder;
        }

        public static void CreateGlobalLoggerConfiguration()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .WriteTo.Console().CreateLogger();
        }
    }
}