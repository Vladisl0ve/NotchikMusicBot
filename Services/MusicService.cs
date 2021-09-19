using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NMB.Handlers;
using Serilog;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace NMB.Services
{
    public class MusicService
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        public bool isPlaying => _lavaNode.Players.Any(p => p.Track != null);
        public double durationTrackSeconds => isPlaying ? _lavaNode.Players.First().Track.Duration.TotalSeconds : 0;
        public bool isLoopedPlaylist = false;
        public bool isLoopedTrack = false;
        private SearchResponse playlistForLoop = new SearchResponse();
        private LavaTrack trackForLoop = null;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
            _lavaNode.OnTrackStarted += TrackStarted;
            _lavaNode.OnTrackEnded += TrackEnded;
            _lavaNode.OnLog += LogMS;
            _client.Ready += OnReadyClient;
        }

        private async Task TrackStarted(TrackStartEventArgs arg)
        {
            var track = arg.Track;
            Log.Information($"Bot Now Playing: {track.Title}");
            if (track.Id == "0s9P1IFxJ0Y") // Id of 'You fucking dead'
                return;

            await arg.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music", $"Now Playing: **{track.Title}**\nUrl: {track.Url}", Color.Blue, track.FetchArtworkAsync().Result));
        }

        private async Task OnReadyClient()
        {
            try
            {
                if (!_lavaNode.IsConnected)
                    await _lavaNode.ConnectAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
        }

        private Task LogMS(LogMessage arg)
        {
            Exception ex = arg.Exception;
            string message = arg.Message;
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    Log.Fatal(ex, message);
                    break;

                case LogSeverity.Error:
                    Log.Error(ex, message);
                    break;

                case LogSeverity.Warning:
                    Log.Warning(ex, message);
                    break;

                case LogSeverity.Info:
                    Log.Information(ex, message);
                    break;

                case LogSeverity.Verbose:
                    Log.Verbose(ex, message);
                    break;

                case LogSeverity.Debug:
                    Log.Debug(ex, message);
                    break;

                default:
                    Log.Fatal("LogMS is down");
                    break;
            }
            return Task.CompletedTask;
        }

        public async Task<Embed> JoinAsync(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", "I'm already connected to a voice channel!");
            }

            if (voiceState.VoiceChannel is null)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", "You must be connected to a voice channel!");
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                return await EmbedHandler.CreateBasicEmbed("Music, Join", $"Joined {voiceState.VoiceChannel.Name}.", Color.Green);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", ex.Message);
            }
        }

        public async Task ForcePlayAsync(SocketGuildUser user, ITextChannel textChannel, string query = "", SearchResponse searchResponse = new(), int numberOfTrack = -1)
        {
            var voiceState = user as IVoiceState;
            if (!CheckIsUserInVoiceChannel(user, textChannel).Result)
                return;

            IGuild guild = voiceState.VoiceChannel.Guild;
            if (!CheckIsPlayerJoined(guild, voiceState, textChannel).Result)
                return;

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);

                //Find The Youtube Track the User requested.
                LavaTrack track;

                var search = searchResponse.Tracks.Count > 0 ?
                    searchResponse :
                    FindTracksAsync(query).Result;

                if (search.Status == SearchStatus.NoMatches)
                {
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music", $"I wasn't able to find anything for {query}."));
                }

                track = numberOfTrack > 0 ?
                    search.Tracks.ElementAt(numberOfTrack - 1) :
                    search.Tracks.FirstOrDefault();

                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(track);
                    Log.Information($"{track.Title} has been added to the queue");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Force Play", $"{track.Title} has been added to queue.", Color.Blue));
                }
                else
                    await player.PlayAsync(track);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music, Play", ex.Message));
            }
        }

        public async Task<SearchResponse> FindTracksAsync(string query)
        {
            var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                await _lavaNode.SearchAsync(SearchType.Direct, query)
                : await _lavaNode.SearchYouTubeAsync(query);

            return search;
        }

        public async Task<bool> TrackChoiceAsync(ITextChannel textChannel, SearchResponse search)
        {
            List<LavaTrack> tracks;

            if (search.Status == SearchStatus.NoMatches)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Searching", $"I wasn't able to find anything."));
                return false;
            }

            if (search.Status == SearchStatus.LoadFailed)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Searching", "Load failed, try again"));
                return false;
            }

            tracks = search.Tracks.Take(5).ToList();
            List<string> tracksToShow = new List<string>();

            foreach (var tracking in tracks)
                tracksToShow.Add($"{tracking.Title.Substring(0, tracking.Title.Count() > 100 ? 100 : tracking.Title.Count())} ({(int)tracking.Duration.TotalHours}:{tracking.Duration.ToString("mm")}:{tracking.Duration.ToString("ss")})");

            await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateListEmbed("Choose the track", tracksToShow, Color.Blue));
            return true;
        }

        private async Task<bool> CheckIsUserInVoiceChannel(SocketGuildUser user, ITextChannel textChannel)
        {
            if (user.VoiceChannel == null)
            {
                Log.Information($"{user.Username} tried start to play not being in voice channel");
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music, Play", "You Must First Join a Voice Channel."));
                return false;
            }

            return true;
        }

        private async Task<bool> CheckIsPlayerJoined(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                try
                {
                    await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                    Log.Information($"Joined to {voiceState.VoiceChannel.Name}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music, Join", ex.Message));
                    return false;
                }
            }
            else
                return true;
        }

        public async Task PlayPlaylistAsync(SocketGuildUser user, ITextChannel textChannel, SearchResponse search)
        {
            var voiceState = user as IVoiceState;
            if (!CheckIsUserInVoiceChannel(user, textChannel).Result)
                return;

            IGuild guild = voiceState.VoiceChannel.Guild;
            if (!CheckIsPlayerJoined(guild, voiceState, textChannel).Result)
                return;

            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (search.Status == SearchStatus.NoMatches)
                {
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music", $"I wasn't able to find anything."));
                }
                playlistForLoop = search;
                trackForLoop = null;
                List<LavaTrack> tracks = search.Tracks.ToList();
                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(tracks);
                    Log.Information($"{search.Playlist.Name} playlist has been added to the music queue.");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music", $"{search.Playlist.Name} playlist has been added to queue.", Color.Blue));
                }
                else
                {
                    var firstTrack = tracks.First();
                    tracks.Remove(firstTrack);
                    player.Queue.Enqueue(tracks);
                    await player.PlayAsync(firstTrack);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Music, Play", ex.Message));
            }
        }

        public async Task SoundPressFAsync(SocketGuildUser user, ITextChannel textChannel, string query = "https://www.youtube.com/watch?v=0s9P1IFxJ0Y")
        {
            var voiceState = user as IVoiceState;
            if (user.VoiceChannel == null)
                return;

            IGuild guild = voiceState.VoiceChannel.Guild;
            if (!CheckIsPlayerJoined(guild, voiceState, textChannel).Result)
                return;

            try
            {
                var player = _lavaNode.GetPlayer(guild);
                LavaTrack track;

                var search = FindTracksAsync(query).Result;

                //If we couldn't find anything, tell the user.
                if (search.Status == SearchStatus.NoMatches)
                {
                    Log.Error($"Youtube pojebało");
                    return;
                }

                track = search.Tracks.First();
                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    Log.Information($"{track.Title} is playing, sorry");
                    return;
                }
                await player.PlayAsync(track);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return;
            }
        }

        public async Task<Embed> NowPlayingAsync(ITextChannel textChannel)
        {
            IGuild guild = textChannel.Guild;
            if (_lavaNode.HasPlayer(guild))
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player.Track != null)
                {
                    return await EmbedHandler.CreateBasicEmbed("Now playing", $"Now Playing: {player.Track.Title}\n{player.Track.Position.ToString(@"hh\:mm\:ss")}/{player.Track.Duration} \nUrl: {player.Track.Url}", Color.Teal, player.Track.FetchArtworkAsync().Result);
                }
            }

            return await EmbedHandler.CreateBasicEmbed("Now playing", "No tracks to play", Color.DarkBlue);
        }

        public async Task LoopAsync(SocketGuildUser user, ITextChannel textChannel)
        {
            var voiceState = user as IVoiceState;
            if (!CheckIsUserInVoiceChannel(user, textChannel).Result)
                return;

            IGuild guild = voiceState.VoiceChannel.Guild;
            if (!CheckIsPlayerJoined(guild, voiceState, textChannel).Result)
                return;

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);

                if (player.Track == null)
                {
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music", $"Nothing is playing", Color.Gold));
                    return;
                }

                if (playlistForLoop.Tracks != null)
                {
                    isLoopedPlaylist = true;
                    isLoopedTrack = false;
                    Log.Information($"Playlist {playlistForLoop.Playlist.Name} looped");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music", $"Looped on playlist: {playlistForLoop.Playlist.Name}", Color.Gold));
                    return;
                }

                if (trackForLoop != null)
                {
                    isLoopedTrack = true;
                    isLoopedPlaylist = false;
                    Log.Information($"Track {trackForLoop.Title} looped");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music", $"Looped on track: {trackForLoop.Title}\n URL: {trackForLoop.Url}", Color.Gold));
                    return;
                }
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Loop", "Nothing to loop"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Loop", ex.Message));
            }
        }

        public async Task ShuffleAsync(SocketGuildUser user, ITextChannel textChannel)
        {
            var voiceState = user as IVoiceState;
            if (!CheckIsUserInVoiceChannel(user, textChannel).Result)
                return;

            IGuild guild = voiceState.VoiceChannel.Guild;
            if (!CheckIsPlayerJoined(guild, voiceState, textChannel).Result)
                return;

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);

                player.Queue.Shuffle();

                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Shuffle", "Shuffled successfully", Color.Magenta));
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Shuffle", ex.Message));
            }
        }

        public async Task LeaveAsync(IGuild guild, bool isF = false)
        {
            try
            {
                LavaPlayer player;
                if (!_lavaNode.TryGetPlayer(guild, out player))
                    return;

                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                await _lavaNode.LeaveAsync(player.VoiceChannel);
                if (isF)
                    return;

                Log.Information($"Bot left");
                await EmbedHandler.CreateBasicEmbed("Music", $"Good bye.", Color.DarkPurple);
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex, ex.Message);
                await EmbedHandler.CreateErrorEmbed("Leave", ex.Message);
            }
        }

        /*This is ran when a user uses the command List
            Task Returns an Embed which is used in the command call. */

        public async Task<Embed> ListAsync(IGuild guild)
        {
            try
            {
                var descriptionBuilder = new StringBuilder();
                LavaPlayer player = null;
                if (!_lavaNode.TryGetPlayer(guild, out player))
                    return await EmbedHandler.CreateErrorEmbed("List", $"Could not aquire player");

                if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    if (player.Queue.Count < 1 && player.Track != null)
                    {
                        return await EmbedHandler.CreateBasicEmbed($"Now Playing: {player.Track.Title}", "Nothing Else Is Queued.", Color.Blue, player.Track.FetchArtworkAsync().Result);
                    }
                    else
                    {
                        var trackNum = 2;
                        foreach (LavaTrack track in player.Queue)
                        {
                            var length = track.Title.Count();
                            descriptionBuilder.Append($"{trackNum}: [{track.Title.Substring(0, length <= 100 ? length : 100)}]({track.Url})\n");
                            trackNum++;

                            if (trackNum == 7)
                            {
                                descriptionBuilder.Append($"There is {player.Queue.Count} songs in queue\n");
                                break;
                            }
                        }
                        return await EmbedHandler.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.Track.Title}]({player.Track.Url}) \n{descriptionBuilder}", Color.Blue, player.Track.FetchArtworkAsync().Result);
                    }
                }
                else
                {
                    return await EmbedHandler.CreateBasicEmbed("List", "Playlist is empty", Color.Magenta);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return await EmbedHandler.CreateErrorEmbed("Music, List", ex.Message);
            }
        }

        public async Task SkipTrackAsync(IGuild guild, ITextChannel textChannel)
        {
            try
            {
                LavaPlayer player = null;
                if (!_lavaNode.TryGetPlayer(guild, out player))
                {
                    Log.Error("Could not aquire player");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("List", $"Could not aquire player"));
                }

                if (player.Queue.Count < 1)
                {
                    if (player.Track == null)
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music Skipped", "Nothing to skip", Color.Gold));
                        return;
                    }

                    if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        await player.StopAsync();
                    }

                    Log.Information("Bots skipped last track");
                    await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Music Skipped", "Track is skipped", Color.Gold));
                }
                else
                {
                    try
                    {
                        await player.SkipAsync();
                        Log.Information("Bots skipped track");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, ex.Message);
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Skip", ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Skip", ex.Message));
            }
        }

        public async Task<Embed> StopAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Stop", $"Could not aquire player");

                if (player.PlayerState is PlayerState.Playing)
                {
                    trackForLoop = null;
                    playlistForLoop = new SearchResponse();
                    await player.StopAsync();
                    player.Queue.Clear();
                    isLoopedPlaylist = false;
                    isLoopedTrack = false;
                }

                Log.Information($"Bot has stopped playback.");
                return await EmbedHandler.CreateBasicEmbed("Music Stop", "Playlist cleared", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Stop", ex.Message);
            }
        }

        /*This is ran when a user uses the command Volume
        Task Returns a String which is used in the command call. */

        public async Task<string> SetVolumeAsync(IGuild guild, int volume)
        {
            if (volume > 150 || volume <= 0)
            {
                return $"Volume must be between 1 and 150.";
            }
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                await player.UpdateVolumeAsync((ushort)volume);
                Log.Information($"Bot Volume set to: {volume}");
                return $"Volume has been set to {volume}.";
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(ex, ex.Message);
                return ex.Message;
            }
        }

        public async Task<Embed> PauseAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (!(player.PlayerState is PlayerState.Playing))
                {
                    await player.PauseAsync();
                    return await EmbedHandler.CreateBasicEmbed("Pause", $"There is nothing to pause.", Color.Orange);
                }

                await player.PauseAsync();
                return await EmbedHandler.CreateBasicEmbed("Paused", $"**{player.Track.Title}**", Color.LighterGrey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return await EmbedHandler.CreateErrorEmbed("Pause", ex.Message);
            }
        }

        public async Task<Embed> ResumeAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player.PlayerState is PlayerState.Paused)
                {
                    await player.ResumeAsync();
                }
                return await EmbedHandler.CreateBasicEmbed("Resumed", $"**{player.Track.Title}**", Color.Green);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return await EmbedHandler.CreateErrorEmbed("Pause", ex.Message);
            }
        }

        private async Task TrackEnded(TrackEndedEventArgs args)
        {
            if (args.Track.Id == "0s9P1IFxJ0Y") //id of 'YOU FUCKING DEAD'
                return;
            string track = args.Track.Title;
            switch (args.Reason)
            {
                case TrackEndReason.Cleanup:
                    //await args.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Track ended", $"Tracks have been cleaned up", Color.Red));
                    Log.Warning($"Track **{track}** has been cleaned up");
                    break;

                case TrackEndReason.Finished:
                    //await args.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Track ended", $"Track's ended", Color.Green));
                    Log.Information($"Track **{track}** ended");
                    break;

                case TrackEndReason.LoadFailed:
                    await args.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Track ended", $"Track **{track}** load failed", Color.DarkRed));
                    Log.Error($"Track **{track}** load failed");
                    break;

                case TrackEndReason.Replaced:
                    await args.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Track ended", $"Track **{track}** has been replaced by new one", Color.Orange));
                    Log.Information($"Track **{track}** has been replaced");
                    return;

                case TrackEndReason.Stopped:
                    await args.Player.TextChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Track ended", $"Track **{track}** stopped", Color.Green));
                    Log.Information($"Track **{track}** stopped");
                    break;
            }

            if (!args.Player.Queue.Any())
            {
                if (isLoopedPlaylist)
                    args.Player.Queue.Enqueue(playlistForLoop.Tracks);
                if (isLoopedTrack)
                    args.Player.Queue.Enqueue(trackForLoop);
            }

            LavaTrack nextTrack = null;
            if (args.Player.Queue.TryDequeue(out nextTrack))
            {
                await args.Player.PlayAsync(nextTrack);
            }

            #region previous_version

            /*
                        var currentTrack = args.Player.Queue.FirstOrDefault();
                        if (args.Reason == TrackEndReason.Replaced && currentTrack != null)
                        {
                            return;
                        }

                        if (args.Player.Queue.Count == 0)
                        {
                            if (isLoopedPlaylist)
                            {
                                args.Player.Queue.Enqueue(playlistForLoop.Tracks);
                                await args.Player.SkipAsync();
                                return;
                            }
                            if (isLoopedTrack)
                            {
                                args.Player.Queue.Enqueue(trackForLoop);
                                await args.Player.SkipAsync();
                                return;
                            }
                        }

                        if (!args.Player.Queue.TryDequeue(out var queueable))
                        {
                            await args.Player.TextChannel.SendMessageAsync(
                                embed: await EmbedHandler.CreateBasicEmbed("Playlist", $"Playback is finished", Color.Green));
                            return;
                        }
                        if (!(queueable is LavaTrack track))
                        {
                            await args.Player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                            return;
                        }

                        await args.Player.PlayAsync(track);
                        await args.Player.TextChannel.SendMessageAsync(
                            embed: await EmbedHandler.CreateBasicEmbed("Now Playing", $"[{track.Title}]({track.Url})", Color.Blue, track.FetchArtworkAsync().Result));
                   */

            #endregion previous_version
        }
    }
}