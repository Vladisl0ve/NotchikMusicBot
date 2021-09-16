using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using NMB.DataStructs;
using NMB.Services;

namespace NMB
{
    public class GlobalData
    {
        public static string ConfigPath { get; set; } = "config.json";
        public static BotConfig Config { get; set; }

        public async Task InitializeAsync()
        {
            string json;

            //Check if Config.json Exists.
            if (!File.Exists(ConfigPath))
            {
                json = JsonConvert.SerializeObject(GenerateNewConfig(), Formatting.Indented);
                File.WriteAllText("config.json", json, new UTF8Encoding(false));
                await LoggingService.LogAsync("Bot", LogSeverity.Error, "No Config file found. A new one has been generated. Please close and fill in the required section.");
                await Task.Delay(-1);
            }

            //If Config.json exists, get the values and apply them to the Global Property (Config).
            json = File.ReadAllText(ConfigPath, new UTF8Encoding(false));
            Config = JsonConvert.DeserializeObject<BotConfig>(json);
        }

        private static BotConfig GenerateNewConfig() => new BotConfig
        {
            DiscordToken = "",
            DefaultPrefix = "!",
            ActivityType = ActivityType.Watching,
            ActivityName = "kak Notchik gorit",
            BlacklistedChannels = new List<ulong>()
        };
    }
}