using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using NMB.Handlers;
using NMB.Services;
using Victoria.Responses.Search;

namespace NMB.Modules
{
    public class MusicModule : InteractiveBase
    {
        public MusicService MusicService { get; set; }

        [Command("Join")]
        public async Task JoinAndPlay()
           => await ReplyAsync(embed: await MusicService.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("Leave")]
        [Alias("Disconnect")]
        public async Task Leave()
            => await MusicService.LeaveAsync(Context.Guild);

        [Command("Play", RunMode = RunMode.Async)]
        [Alias("p")]
        public async Task Play([Remainder] string search)
        {
            var searchResponse = await MusicService.FindPlaylistAsync(search);
            if (searchResponse.Status == SearchStatus.PlaylistLoaded)
            {
                await ReplyAsync(embed: await MusicService.PlayPlaylistAsync(Context.User as SocketGuildUser, Context.User as IVoiceState, Context.Channel as ITextChannel, searchResponse));
                return;
            }

            await ReplyAsync(embed: await MusicService.FindTracksAsync(search));
            var response = await NextMessageAsync();
            if (response != null)
            {
                if (int.TryParse(response.Content, out int responseInt) && responseInt > 0 && responseInt <= 5)
                    await ReplyAsync(embed: await MusicService.PlayChosenTrackAsync(Context.User as SocketGuildUser, Context.User as IVoiceState, Context.Channel as ITextChannel, search, responseInt));
                else
                    await ReplyAsync(embed: await EmbedHandler.CreateErrorEmbed("Music, choice of", "Use int only in (0; 5] area"));
            }
        }

        [Command("f", RunMode = RunMode.Async)]
        public async Task PressF()
        {
            await ReplyAsync("F");
            await MusicService.PressFAsync(Context.User as SocketGuildUser, Context.User as IVoiceState, Context.Channel as ITextChannel);

            var start = DateTime.Now;
            var stop = DateTime.Now.AddSeconds(MusicService.durationTrackSeconds + 1);

            while (DateTime.Now < stop)
            {
            }
            await MusicService.LeaveAsync(Context.Guild, true);
        }

        [Command("FPlay")]
        [Alias("fp")]
        public async Task ForcePlay([Remainder] string search)
            => await ReplyAsync(embed: await MusicService.ForcePlayAsync(Context.User as SocketGuildUser, Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel, search));

        [Command("Stop")]
        public async Task Stop()
            => await ReplyAsync(embed: await MusicService.StopAsync(Context.Guild));

        [Command("List")]
        [Alias("Queue")]
        public async Task List()
            => await ReplyAsync(embed: await MusicService.ListAsync(Context.Guild));

        [Command("Skip")]
        [Alias("s")]
        public async Task Skip()
            => await ReplyAsync(embed: await MusicService.SkipTrackAsync(Context.Guild));

        [Command("Loop")]
        [Alias("l")]
        public async Task Loop() => await ReplyAsync(embed: await MusicService.LoopAsync(Context.User as SocketGuildUser, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("Shuffle")]
        public async Task Suffle() => await ReplyAsync(embed: await MusicService.ShuffleAsync(Context.User as SocketGuildUser, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("Volume")]
        [Alias("v")]
        public async Task Volume(int volume)
            => await ReplyAsync(await MusicService.SetVolumeAsync(Context.Guild, volume));

        [Command("Pause")]
        public async Task Pause()
            => await ReplyAsync(await MusicService.PauseAsync(Context.Guild));

        [Command("Resume")]
        public async Task Resume()
            => await ReplyAsync(await MusicService.ResumeAsync(Context.Guild));
    }
}