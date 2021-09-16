using System.Collections.Generic;
using Discord;

namespace NMB.DataStructs
{
    public class BotConfig
    {
        public string DiscordToken { get; set; }
        public string DefaultPrefix { get; set; }
        public ActivityType ActivityType { get; set; }
        public string ActivityName { get; set; }
        public List<ulong> BlacklistedChannels { get; set; }
    }
}