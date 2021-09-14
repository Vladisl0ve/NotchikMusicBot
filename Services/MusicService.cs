using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        private string choice = null;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;
        }

        private Task ChoiceReceived(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
                return Task.CompletedTask;
            if (message.Source != MessageSource.User)
                return Task.CompletedTask;
            var argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos))
                return Task.CompletedTask;
            var context = new SocketCommandContext(_client, message);
            choice = message.Content;
            return Task.CompletedTask;
        }
        public async Task<Embed> JoinAsync(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Join", "I'm already connected to a voice channel!");
            }

            if (voiceState.VoiceChannel is null)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Join", "You must be connected to a voice channel!");
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                return await EmbedHandlingService.CreateBasicEmbed("Music, Join", $"Joined {voiceState.VoiceChannel.Name}.", Color.Green);
            }
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Join", ex.Message);
            }
        }

        public async Task<Embed> ForcePlayAsync(SocketGuildUser user, IGuild guild, IVoiceState voiceState, ITextChannel textChannel, string query)
        {
            //Check If User Is Connected To Voice Cahnnel.
            if (user.VoiceChannel == null)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Play", "You Must First Join a Voice Channel.");
            }

            //Check the guild has a player available.
            if (!_lavaNode.HasPlayer(guild))
            {
                try
                {
                    await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                }
                catch (Exception ex)
                {
                    return await EmbedHandlingService.CreateErrorEmbed("Music, Join", ex.Message);
                }
            }

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);

                //Find The Youtube Track the User requested.
                LavaTrack track;

                var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, query)
                    : await _lavaNode.SearchYouTubeAsync(query);

                //If we couldn't find anything, tell the user.
                if (search.Status == SearchStatus.NoMatches)
                {
                    return await EmbedHandlingService.CreateErrorEmbed("Music", $"I wasn't able to find anything for {query}.");
                }

                //Get the first track from the search results.
                //TODO: Add a 1-5 list for the user to pick from. (Like Fredboat)
                track = search.Tracks.FirstOrDefault();

                //If the Bot is already playing music, or if it is paused but still has music in the playlist, Add the requested track to the queue.
                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(track);
                    await LoggingService.LogInformationAsync("Music", $"{track.Title} has been added to the music queue.");
                    return await EmbedHandlingService.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.", Color.Blue);
                }

                //Player was not playing anything, so lets play the requested track.
                await player.PlayAsync(track);
                await LoggingService.LogInformationAsync("Music", $"Bot Now Playing: {track.Title}\nUrl: {track.Url}");
                return await EmbedHandlingService.CreateBasicEmbed("Music", $"Now Playing: {track.Title}\nUrl: {track.Url}", Color.Blue);
            }

            //If after all the checks we did, something still goes wrong. Tell the user about it so they can report it back to us.
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Play", ex.Message);
            }

        }

        //Play with list of tracks
        public async Task<Embed> PlayAsync(SocketGuildUser user, IGuild guild, IVoiceState voiceState, ITextChannel textChannel, string query)
        {
            //Check If User Is Connected To Voice Cahnnel.
            if (user.VoiceChannel == null)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Play", "You Must First Join a Voice Channel.");
            }

            //Check the guild has a player available.
            if (!_lavaNode.HasPlayer(guild))
            {
                try
                {
                    await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                }
                catch (Exception ex)
                {
                    return await EmbedHandlingService.CreateErrorEmbed("Music, Join", ex.Message);
                }
            }

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);
                //Find The Youtube Track the User requested.
                List<LavaTrack> tracks;

                var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, query)
                    : await _lavaNode.SearchYouTubeAsync(query);

                //If we couldn't find anything, tell the user.
                if (search.Status == SearchStatus.NoMatches)
                {
                    return await EmbedHandlingService.CreateErrorEmbed("Music", $"I wasn't able to find anything for {query}.");
                }

                //Get the first track from the search results.
                //TODO: Add a 1-5 list for the user to pick from. (Like Fredboat)
                tracks = search.Tracks.Take(5).ToList();
                List<string> tracksToShow = new List<string>();

                foreach (var tracking in tracks)
                    tracksToShow.Add(tracking.Title);

                await EmbedHandlingService.CreateListEmbed("Choose the track", tracksToShow, Color.Blue);
                var track = tracks[int.Parse(choice)];
                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(track);
                    await LoggingService.LogInformationAsync("Music", $"{track.Title} has been added to the music queue.");
                    return await EmbedHandlingService.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.", Color.Blue);
                }

                //Player was not playing anything, so lets play the requested track.
                await player.PlayAsync(track);
                await LoggingService.LogInformationAsync("Music", $"Bot Now Playing: {track.Title}\nUrl: {track.Url}");
                return await EmbedHandlingService.CreateBasicEmbed("Music", $"Now Playing: {track.Title}\nUrl: {track.Url}", Color.Blue);

            }
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Play", ex.Message);
            }


        }


        /*This is ran when a user uses the command Leave.
           Task Returns an Embed which is used in the command call. */
        public async Task<Embed> LeaveAsync(IGuild guild)
        {
            try
            {
                //Get The Player Via GuildID.
                var player = _lavaNode.GetPlayer(guild);

                //if The Player is playing, Stop it.
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                //Leave the voice channel.
                await _lavaNode.LeaveAsync(player.VoiceChannel);

                await LoggingService.LogInformationAsync("Music", $"Bot has left.");
                return await EmbedHandlingService.CreateBasicEmbed("Music", $"I've left. Thank you for playing moosik.", Color.Blue);
            }
            //Tell the user about the error so they can report it back to us.
            catch (InvalidOperationException ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Leave", ex.Message);
            }
        }

        /*This is ran when a user uses the command List 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> ListAsync(IGuild guild)
        {
            try
            {
                /* Create a string builder we can use to format how we want our list to be displayed. */
                var descriptionBuilder = new StringBuilder();

                /* Get The Player and make sure it isn't null. */
                var player = _lavaNode.GetPlayer(guild);
                if (player == null)
                    return await EmbedHandlingService.CreateErrorEmbed("Music, List", $"Could not aquire player");
                if (player.PlayerState is PlayerState.Playing)
                {
                    /*If the queue count is less than 1 and the current track IS NOT null then we wont have a list to reply with.
                        In this situation we simply return an embed that displays the current track instead. */
                    if (player.Queue.Count < 1 && player.Track != null)
                    {
                        return await EmbedHandlingService.CreateBasicEmbed($"Now Playing: {player.Track.Title}", "Nothing Else Is Queued.", Color.Blue);
                    }
                    else
                    {
                        /* Now we know if we have something in the queue worth replying with, so we itterate through all the Tracks in the queue.
                         *  Next Add the Track title and the url however make use of Discords Markdown feature to display everything neatly.
                            This trackNum variable is used to display the number in which the song is in place. (Start at 2 because we're including the current song.*/
                        var trackNum = 2;
                        foreach (LavaTrack track in player.Queue)
                        {
                            descriptionBuilder.Append($"{trackNum}: [{track.Title}]({track.Url}) - {track.Id}\n");
                            trackNum++;
                        }
                        return await EmbedHandlingService.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.Track.Title}]({player.Track.Url}) \n{descriptionBuilder}", Color.Blue);
                    }
                }
                else
                {
                    return await EmbedHandlingService.CreateErrorEmbed("Music, List", "Player doesn't seem to be playing anything right now. If this is an error, Please Contact Vladislove.");
                }
            }
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, List", ex.Message);
            }

        }

        /*This is ran when a user uses the command Skip 
           Task Returns an Embed which is used in the command call. */
        public async Task<Embed> SkipTrackAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                /* Check if the player exists */
                if (player == null)
                    return await EmbedHandlingService.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check !Help for info on how to use the bot.");
                /* Check The queue, if it is less than one (meaning we only have the current song available to skip) it wont allow the user to skip.
                     User is expected to use the Stop command if they're only wanting to skip the current song. */
                if (player.Queue.Count < 1)
                {
                    if (player.PlayerState is PlayerState.Playing)
                    {
                        await player.StopAsync();
                    }

                    await LoggingService.LogInformationAsync("Music", $"Bot has stopped playback.");
                    return await EmbedHandlingService.CreateBasicEmbed("Music Skipped", "Track is skipped", Color.Blue);

                }
                else
                {
                    try
                    {
                        /* Save the current song for use after we skip it. */
                        var currentTrack = player.Track;
                        /* Skip the current song. */
                        await player.SkipAsync();
                        await LoggingService.LogInformationAsync("Music", $"Bot skipped: {currentTrack.Title}");
                        return await EmbedHandlingService.CreateBasicEmbed("Music Skip", $"I have successfully skiped {currentTrack.Title}", Color.Blue);
                    }
                    catch (Exception ex)
                    {
                        return await EmbedHandlingService.CreateErrorEmbed("Music, Skip", ex.Message);
                    }

                }
            }
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Skip", ex.Message);
            }
        }


        /*This is ran when a user uses the command Stop 
    Task Returns an Embed which is used in the command call. */
        public async Task<Embed> StopAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player == null)
                    return await EmbedHandlingService.CreateErrorEmbed("Music, List", $"Could not aquire player");

                /* Check if the player exists, if it does, check if it is playing.
                     If it is playing, we can stop.*/
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                await LoggingService.LogInformationAsync("Music", $"Bot has stopped playback.");
                return await EmbedHandlingService.CreateBasicEmbed("Music Stop", "I Have stopped playback & the playlist has been cleared.", Color.Blue);
            }
            catch (Exception ex)
            {
                return await EmbedHandlingService.CreateErrorEmbed("Music, Stop", ex.Message);
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
                await LoggingService.LogInformationAsync("Music", $"Bot Volume set to: {volume}");
                return $"Volume has been set to {volume}.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> PauseAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (!(player.PlayerState is PlayerState.Playing))
                {
                    await player.PauseAsync();
                    return $"There is nothing to pause.";
                }

                await player.PauseAsync();
                return $"**Paused:** {player.Track.Title}. MB Shreksophone next?";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> ResumeAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player.PlayerState is PlayerState.Paused)
                {
                    await player.ResumeAsync();
                }

                return $"**Resumed:** {player.Track.Title}";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task TrackEnded(TrackEndedEventArgs args)
        {
            if (args.Reason == TrackEndReason.Finished)
            {
                return;
            }

            if (!args.Player.Queue.TryDequeue(out var queueable))
            {
                //await args.Player.TextChannel.SendMessageAsync("Playback Finished.");
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                await args.Player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync(
                embed: await EmbedHandlingService.CreateBasicEmbed("Now Playing", $"[{track.Title}]({track.Url})", Color.Blue));
        }
    }
}
