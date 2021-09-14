using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NMB.Services;

namespace NMB.Modules
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        public MusicService MusicSevice { get; set; }

        [Command("Join")]
        public async Task JoinAndPlay()
           => await ReplyAsync(embed: await MusicSevice.JoinAsync(Context.Guild, Context.User as IVoiceState, Context.Channel as ITextChannel));

        [Command("Leave")]
        [Alias("Disconnect")]
        public async Task Leave()
            => await ReplyAsync(embed: await MusicSevice.LeaveAsync(Context.Guild));

        [Command("Play")]
        public async Task Play([Remainder] string search)
            => await ReplyAsync(embed: await MusicSevice.PlayAsync(Context.User as SocketGuildUser, Context.Guild, search));

        [Command("Stop")]
        public async Task Stop()
            => await ReplyAsync(embed: await MusicSevice.StopAsync(Context.Guild));

        [Command("List")]
        [Alias("Queue")]
        public async Task List()
            => await ReplyAsync(embed: await MusicSevice.ListAsync(Context.Guild));

        [Command("Skip")]
        public async Task Skip()
            => await ReplyAsync(embed: await MusicSevice.SkipTrackAsync(Context.Guild));

        [Command("Volume")]
        public async Task Volume(int volume)
            => await ReplyAsync(await MusicSevice.SetVolumeAsync(Context.Guild, volume));

        [Command("Pause")]
        public async Task Pause()
            => await ReplyAsync(await MusicSevice.PauseAsync(Context.Guild));

        [Command("Resume")]
        public async Task Resume()
            => await ReplyAsync(await MusicSevice.ResumeAsync(Context.Guild));
    }
}
