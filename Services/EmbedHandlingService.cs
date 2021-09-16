using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace NMB.Services
{
    public static class EmbedHandlingService //Embed wrapper
    {
        public static async Task<Embed> CreateBasicEmbed(string title, string description, Color color, string pictureUrl = null)
        {
            Embed embed;

            if (pictureUrl != null)
                embed = await Task.Run(() => (new EmbedBuilder()
                                                .WithTitle(title)
                                                .WithDescription(description)
                                                .WithColor(color)
                                                .WithImageUrl(pictureUrl)
                                                .Build()));
            else
                embed = await Task.Run(() => (new EmbedBuilder()
                                                .WithTitle(title)
                                                .WithDescription(description)
                                                .WithColor(color)
                                                .Build()));
            return embed;
        }

        public static async Task<Embed> CreateErrorEmbed(string source, string error)
        {
            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle($"ERROR OCCURED FROM - {source}")
                .WithDescription($"**Error Details**: \n{error}")
                .WithColor(Color.DarkRed).Build());
            return embed;
        }

        public static async Task<Embed> CreateListEmbed(string title, List<string> elementsToShow, Color color)
        {
            StringBuilder description = new StringBuilder();
            for (int i = 0; i < elementsToShow.Count; i++)
                description.Append($"{i + 1}. {elementsToShow[i]}\n");

            var embed = await Task.Run(() => new EmbedBuilder()
                        .WithTitle(title)
                        .WithDescription(description.ToString())
                        .WithColor(color)
                        .Build());
            return embed;
        }
    }
}