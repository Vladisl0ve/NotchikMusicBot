using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using NMB.Services;

namespace NMB.Modules
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        public MusicService MusicService { get; set; }

        [Command("Join")]
        public async Task JoinAndPlay()
           => await ReplyAsync(embed: await MusicService.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("Leave")]
        [Alias("Disconnect")]
        public async Task Leave()
            => await ReplyAsync(embed: await MusicService.LeaveAsync(Context.Guild));


        [Command("fPlay")]
        [Alias("fp")]
        public async Task Play([Remainder] string search)
        {
            await ReplyAsync(embed: await MusicService.FindTracksAsync(Context.User as SocketGuildUser, Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel, search));

            //await ReplyAsync(embed: await MusicService.PlayAsync(Context.User as SocketGuildUser, Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel, search));
        }

        [Command("Play")]
        [Alias("p")]
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
