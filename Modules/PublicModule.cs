using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace NMB.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync() => ReplyAsync("pong!", isTTS: true);

        [Command("1")]
        [Alias("2", "3", "4", "5")]
        public Task ChoiceAsync([Remainder] string text) => ReplyAsync(text);

        [Command("last")]
        public async Task LastAsync(IUserMessage userMessage = null)
        {
            userMessage = userMessage ?? Context.Message;
            await ReplyAsync(userMessage.ToString());
        }

        // Get info on a user, or the user who invoked the command if one is not specified
        [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("echo")]
        public Task EchoAsync([Remainder] string text)
            // Insert a ZWSP before the text to prevent triggering other bots!
            => ReplyAsync('\u200B' + text);

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");
    }
}